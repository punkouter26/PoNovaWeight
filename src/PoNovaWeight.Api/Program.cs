using Azure.AI.OpenAI;
using Azure.Data.Tables;
using Azure.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PoNovaWeight.Api.Features.Auth;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Features.MealScan;
using PoNovaWeight.Api.Features.WeeklySummary;
using PoNovaWeight.Api.Features.Settings;
using PoNovaWeight.Api.Features.Predictions;
using PoNovaWeight.Api.Infrastructure;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.Validation;
using Scalar.AspNetCore;
using Serilog;

// Configure Serilog before building the host
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/bootstrap-.txt", rollingInterval: RollingInterval.Day)
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
    var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
    if (!string.IsNullOrEmpty(keyVaultUri) && !builder.Environment.IsDevelopment())
    {
        try
        {
            // Configure credential options for better local dev experience
            // Exclude credentials that are problematic or require additional packages
            var credentialOptions = new DefaultAzureCredentialOptions
            {
                ExcludeVisualStudioCodeCredential = true, // Requires Azure.Identity.Broker package
                ExcludeInteractiveBrowserCredential = true
            };
            
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential(credentialOptions));
            Log.Information("Azure Key Vault configured: {VaultUri}", keyVaultUri);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to connect to Azure Key Vault ({VaultUri}). Falling back to user secrets.", keyVaultUri);
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

    // Configure Data Protection to persist keys in Azure Blob Storage (production only)
    // This ensures cookies remain valid across container restarts and scaling
    var dataProtectionBlobUri = builder.Configuration["DataProtection:BlobUri"];
    if (!string.IsNullOrEmpty(dataProtectionBlobUri) && !builder.Environment.IsDevelopment())
    {
        builder.Services.AddDataProtection()
            .SetApplicationName("PoNovaWeight")
            .PersistKeysToAzureBlobStorage(new Uri(dataProtectionBlobUri), new DefaultAzureCredential());
        Log.Information("Data Protection configured with Azure Blob Storage: {BlobUri}", dataProtectionBlobUri);
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
    
    // Configure OpenTelemetry for distributed tracing
    // Creates instrumentation for ASP.NET Core, HTTP client requests
    var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
    if (!string.IsNullOrEmpty(otlpEndpoint))
    {
        // Extension methods from OpenTelemetry.Extensions.Hosting package
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: "PoNovaWeight.Api", serviceVersion: "1.0.0", serviceInstanceId: Environment.MachineName))
            .WithTracing(tracingBuilder =>
            {
                tracingBuilder
                    .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                    .AddHttpClientInstrumentation(options => options.RecordException = true)
                    .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
            });
        Log.Information("OpenTelemetry configured with OTLP Exporter: {Endpoint}", otlpEndpoint);
    }
    else
    {
        Log.Information("OpenTelemetry OTLP endpoint not configured - distributed tracing disabled");
    }
    
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // Response compression for Blazor WASM assets - Simplified to Brotli only
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            ["application/octet-stream", "application/wasm"]);
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);

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

    // Simple in-memory caching - simpler than HybridCache for this use case
    builder.Services.AddMemoryCache();
    
    // Add Health Checks
    builder.Services.AddHealthChecks();

    // Table Storage configuration
    // Reads from PoNovaWeight:AzureStorage:ConnectionString (Key Vault) or ConnectionStrings:AzureStorage (local/fallback)
    var connectionString = builder.Configuration["PoNovaWeight:AzureStorage:ConnectionString"]
        ?? builder.Configuration.GetConnectionString("AzureStorage")
        ?? throw new InvalidOperationException(
            "Azure Storage connection string is required. Set 'ConnectionStrings:AzureStorage' in configuration.");
    builder.Services.AddSingleton(new TableServiceClient(connectionString));

    // Register repositories using TableServiceClient
    builder.Services.AddSingleton<IDailyLogRepository, DailyLogRepository>();
    builder.Services.AddSingleton<IUserRepository, UserRepository>();
    builder.Services.AddSingleton<IUserSettingsRepository, UserSettingsRepository>();

    // Development test-user data generation service (used by /api/auth/dev-test-user-login)
    builder.Services.AddSingleton<ITestUserDataSeeder, TestUserDataSeeder>();

    // Google OAuth + Cookie Authentication
    builder.Services.AddNovaAuthentication(builder.Configuration, builder.Environment);

    // Azure OpenAI - optional for local development (meal scan will be disabled)
    var openAiEndpoint = builder.Configuration["PoNovaWeight:AzureOpenAI:Endpoint"]
        ?? builder.Configuration["AzureOpenAI:Endpoint"];
    var openAiApiKey = builder.Configuration["PoNovaWeight:AzureOpenAI:ApiKey"]
        ?? builder.Configuration["AzureOpenAI:ApiKey"];

    if (!string.IsNullOrEmpty(openAiEndpoint) && !string.IsNullOrEmpty(openAiApiKey) && !openAiApiKey.StartsWith("YOUR-"))
    {
        builder.Services.AddSingleton(new AzureOpenAIClient(
            new Uri(openAiEndpoint),
            new Azure.AzureKeyCredential(openAiApiKey)));
        builder.Services.AddSingleton<IMealAnalysisService, MealAnalysisService>();
        builder.Services.AddSingleton<IBpPredictionService, BpPredictionService>();
    }
    else
    {
        // Use stub services when OpenAI is not configured
        builder.Services.AddSingleton<IMealAnalysisService, StubMealAnalysisService>();
        builder.Services.AddSingleton<IBpPredictionService, StubBpPredictionService>();
        Log.Warning("Azure OpenAI not configured - meal scanning and BP predictions will return mock data");
    }

    // Exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Output caching for read-only endpoints
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));
    });

    // Configure forwarded headers for running behind reverse proxy (Azure Container Apps)
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

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

    // Azure App Service may use X-ARR-SSL; normalize scheme to https so Secure cookies are issued
    app.Use((context, next) =>
    {
        // Explicitly handle X-Forwarded-Proto for Container Apps / Linux App Service
        // This ensures the app knows it's running over HTTPS, which is critical for:
        // 1. Setting Secure cookies
        // 2. Generating correct OAuth redirect URIs (https://...)
        // 3. Validating OAuth tokens during callback
        if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) && 
            string.Equals(proto, "https", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Scheme = "https";
        }

        if (context.Request.Headers.ContainsKey("X-ARR-SSL") &&
            !string.Equals(context.Request.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Scheme = "https";
        }

        return next();
    });

    // Initialize Table Storage (create tables if not exist)
    using (var scope = app.Services.CreateScope())
    {
        var dailyLogRepo = scope.ServiceProvider.GetRequiredService<IDailyLogRepository>() as DailyLogRepository;
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>() as UserRepository;
        var settingsRepo = scope.ServiceProvider.GetRequiredService<IUserSettingsRepository>() as UserSettingsRepository;

        await Task.WhenAll(
            dailyLogRepo?.InitializeAsync() ?? Task.CompletedTask,
            userRepo?.InitializeAsync() ?? Task.CompletedTask,
            settingsRepo?.InitializeAsync() ?? Task.CompletedTask
        );
    }

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
    app.UseResponseCompression();
    app.UseHttpsRedirection();
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();
    app.UseRouting();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Output caching (after auth so cache varies by user)
    app.UseOutputCache();

    // Health Checks
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/alive");

    // Map feature endpoints
    app.MapAuthEndpoints(app.Environment);
    app.MapDailyLogEndpoints();
    app.MapSettingsEndpoints();
    app.MapPredictionsEndpoints();
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
