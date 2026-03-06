using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using PoNovaWeight.Api.Features.Auth;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Features.MealScan;
using PoNovaWeight.Api.Features.WeeklySummary;
using PoNovaWeight.Api.Features.Settings;
using PoNovaWeight.Api.Infrastructure;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.Validation;
using Scalar.AspNetCore;
using Serilog;

// Configure Serilog before building the host
// Bootstrap logger: Console only (no File sink to avoid issues with read-only
// WEBSITE_RUN_FROM_PACKAGE filesystem mounts in Azure App Service).
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configuration priority (highest wins):
    // 1. Environment variables
    // 2. User secrets (development only) - fallback for local dev
    // 3. Azure Key Vault - primary source for secrets (skipped in Development/Test to avoid auth failures)
    // 4. appsettings.{Environment}.json
    // 5. appsettings.json

    // Add Azure Key Vault configuration (Production/Staging only)
    // Uses DefaultAzureCredential which works with:
    // - Local: Azure CLI, Visual Studio, VS Code credentials
    // - Azure: Managed Identity
    // Skip Key Vault in Development/Test environments where authentication is not available
    var keyVaultUri = builder.Configuration["KeyVault:VaultUri"]
        ?? builder.Configuration["KeyVault:Url"];
    if (!string.IsNullOrEmpty(keyVaultUri) && !builder.Environment.IsDevelopment())
    {
        try
        {
            // In Production (Azure App Service with Managed Identity), use ManagedIdentityCredential
            // directly. DefaultAzureCredential tries WorkloadIdentityCredential first, which can
            // take 2+ minutes to fail before falling back to ManagedIdentity — consuming the
            // entire 230-second App Service startup probe window.
            TokenCredential kvCredential;
            if (builder.Environment.IsProduction())
            {
                kvCredential = new ManagedIdentityCredential();
            }
            else
            {
                kvCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeVisualStudioCodeCredential = true,
                    ExcludeInteractiveBrowserCredential = true
                });
            }

            builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), kvCredential);
            Log.Information("Azure Key Vault configured: {VaultUri}", keyVaultUri);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to connect to Azure Key Vault ({VaultUri}). Falling back to environment variables.", keyVaultUri);
        }
    }
    else if (builder.Environment.IsDevelopment())
    {
        Log.Information("Key Vault skipped in Development environment - using appsettings and environment variables only");
    }
    else
    {
        Log.Information("Key Vault not configured - using user secrets and environment variables only");
    }

    // Serilog configuration
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    builder.Services.AddApplicationInsightsTelemetry();

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // MediatR with validation pipeline
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });

    // FluentValidation - register validators from Shared assembly
    builder.Services.AddValidatorsFromAssemblyContaining<DailyLogDtoValidator>();

    // TimeProvider for testable time abstractions
    builder.Services.AddSingleton(TimeProvider.System);

    // Add Health Checks
    builder.Services.AddHealthChecks();

    // Table Storage — Key Vault (prod) or appsettings (dev/Azurite)
    var tableStorageValue = builder.Configuration.GetConnectionString("AzureStorage")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:AzureStorage is required. Configure via Key Vault or appsettings.");

    var tableServiceClient = Uri.TryCreate(tableStorageValue, UriKind.Absolute, out var tableEndpoint)
        && tableEndpoint.Scheme is "https" or "http"
        ? new TableServiceClient(tableEndpoint, new ManagedIdentityCredential())
        : new TableServiceClient(tableStorageValue);

    builder.Services.AddSingleton(tableServiceClient);

    // Register repositories using TableServiceClient
    builder.Services.AddSingleton<IDailyLogRepository, DailyLogRepository>();
    builder.Services.AddSingleton<IUserRepository, UserRepository>();
    builder.Services.AddSingleton<IUserSettingsRepository, UserSettingsRepository>();

    // Initialize Table Storage tables in the background so the warm-up probe
    // succeeds immediately instead of blocking the 230-second startup window.
    builder.Services.AddHostedService<BackgroundTableInitService>();

    // Development test-user data generation service (used by /api/auth/dev-test-user-login)
    builder.Services.AddSingleton<ITestUserDataSeeder, TestUserDataSeeder>();

    // Google OAuth + Cookie Authentication
    builder.Services.AddNovaAuthentication(builder.Configuration, builder.Environment);

    // Azure OpenAI — required for meal scanning
    var openAiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"]
        ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required. Configure via Key Vault or appsettings.");
    var openAiApiKey = builder.Configuration["AzureOpenAI:ApiKey"]
        ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required. Configure via Key Vault or appsettings.");

    builder.Services.AddSingleton(new AzureOpenAIClient(
        new Uri(openAiEndpoint),
        new Azure.AzureKeyCredential(openAiApiKey)));
    builder.Services.AddSingleton<IMealAnalysisService, MealAnalysisService>();

    // Exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // Use forwarded headers (must be first in pipeline)
    app.UseForwardedHeaders();

    // Correlation ID middleware - extract or generate correlation ID for distributed tracing
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Session middleware must be registered before any middleware tries to access context.Session
    app.UseSession();

    // Log Enrichment Middleware (Standard 3)
    app.Use(async (context, next) =>
    {
        var userId = context.User.Identity?.Name ?? "Anonymous";
        var sessionId = context.Session?.Id ?? context.Request.Cookies["ASP.NET_SessionId"] ?? "NoSession";
        
        using (Serilog.Context.LogContext.PushProperty("UserId", userId))
        using (Serilog.Context.LogContext.PushProperty("SessionId", sessionId))
        using (Serilog.Context.LogContext.PushProperty("Environment", builder.Environment.EnvironmentName))
        {
            await next();
        }
    });

   

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        // OpenAPI spec at /openapi/v1.json + Scalar UI at /scalar/v1
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.UseWebAssemblyDebugging();
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();
    app.UseRouting();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Health Checks
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/alive");

    // Map feature endpoints
    app.MapAuthEndpoints(app.Environment);
    app.MapDailyLogEndpoints();
    app.MapSettingsEndpoints();
    app.MapWeeklySummaryEndpoints();
    app.MapMealScanEndpoints();
    app.MapControllers();
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Partial class for WebApplicationFactory integration tests.
/// </summary>
public partial class Program { }
