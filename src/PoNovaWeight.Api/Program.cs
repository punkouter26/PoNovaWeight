using Azure.AI.OpenAI;
using Azure.Data.Tables;
using Azure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using PoNovaWeight.Api.Features.Auth;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Features.MealScan;
using PoNovaWeight.Api.Features.WeeklySummary;
using PoNovaWeight.Api.Infrastructure;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.Validation;
using Scalar.AspNetCore;
using Serilog;
using System.Security.Claims;

// Configure Serilog before building the host
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configuration priority (highest wins):
    // 1. Environment variables
    // 2. User secrets (development only) - fallback for local dev
    // 3. Azure Key Vault - primary source for secrets
    // 4. appsettings.{Environment}.json
    // 5. appsettings.json

    // Add Azure Key Vault configuration
    // Uses DefaultAzureCredential which works with:
    // - Local: Azure CLI, Visual Studio, VS Code credentials
    // - Azure: Managed Identity
    var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
    if (!string.IsNullOrEmpty(keyVaultUri))
    {
        try
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
            Log.Information("Azure Key Vault configured: {VaultUri}", keyVaultUri);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to connect to Azure Key Vault ({VaultUri}). Falling back to user secrets.", keyVaultUri);
        }
    }
    else
    {
        Log.Information("Key Vault not configured - using user secrets and environment variables only");
    }

    // Add Aspire service defaults for OpenTelemetry, health checks, and resilience
    builder.AddServiceDefaults();

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

    // Response compression for Blazor WASM assets
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            ["application/octet-stream", "application/wasm"]);
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);

    // MediatR
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
    });

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<DailyLogDtoValidator>();

    // Azure Table Storage - Use Aspire integration when available
    // Check if running under Aspire orchestration
    var isAspireOrchestrated = !string.IsNullOrEmpty(builder.Configuration["ConnectionStrings:tables"]) ||
                                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));

    if (isAspireOrchestrated)
    {
        // Aspire integration - automatic connection via service discovery
        builder.AddAzureTableServiceClient("tables", settings =>
        {
            // Disable health checks due to bug in AspNetCore.HealthChecks.Azure.Data.Tables
            // that sends invalid OData filter ($filter=false) causing 400 errors
            settings.DisableHealthChecks = true;
            settings.DisableTracing = false;
        });
    }
    else
    {
        // Non-Aspire scenarios (tests, standalone) - require explicit connection string
        // Reads from PoNovaWeight:AzureStorage:ConnectionString (Key Vault) or ConnectionStrings:AzureStorage (local/fallback)
        var connectionString = builder.Configuration["PoNovaWeight:AzureStorage:ConnectionString"]
            ?? builder.Configuration.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException(
                "Azure Storage connection string is required. Set 'ConnectionStrings:AzureStorage' in configuration or run with Aspire.");
        builder.Services.AddSingleton(new TableServiceClient(connectionString));
    }

    // Register repositories using TableServiceClient
    builder.Services.AddSingleton<IDailyLogRepository, DailyLogRepository>();
    builder.Services.AddSingleton<IUserRepository, UserRepository>();

    // Google OAuth + Cookie Authentication
    // Reads from PoNovaWeight:Google:* (Key Vault) or Google:* (local/fallback)
    var googleClientId = builder.Configuration["PoNovaWeight:Google:ClientId"]
        ?? builder.Configuration["Google:ClientId"];
    var googleClientSecret = builder.Configuration["PoNovaWeight:Google:ClientSecret"]
        ?? builder.Configuration["Google:ClientSecret"];

    var hasGoogleCredentials = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);

    if (!hasGoogleCredentials && !builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Google OAuth credentials are required in production. Set Google:ClientId and Google:ClientSecret in configuration.");
    }

    var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = hasGoogleCredentials ? GoogleDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "nova-session";
        options.Cookie.HttpOnly = true;
        // Use SameAsRequest in development (HTTP), Always in production (HTTPS)
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
            ? CookieSecurePolicy.SameAsRequest 
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax; // Lax required for OAuth redirects
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            // Return 401 for API calls instead of redirecting
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

    // Add Google OAuth only when credentials are available
    if (hasGoogleCredentials)
    {
        authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = googleClientId!;
                options.ClientSecret = googleClientSecret!;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = false;
                options.Events.OnCreatingTicket = async context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
                    var name = context.Principal?.FindFirstValue(ClaimTypes.Name);
                    var picture = context.Principal?.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

                    if (!string.IsNullOrEmpty(email))
                    {
                        var userRepo = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                        var existingUser = await userRepo.GetAsync(email);

                        if (existingUser is not null)
                        {
                            // Update last login time
                            existingUser.LastLoginUtc = DateTimeOffset.UtcNow;
                            existingUser.DisplayName = name ?? existingUser.DisplayName;
                            existingUser.PictureUrl = picture ?? existingUser.PictureUrl;
                            await userRepo.UpsertAsync(existingUser);
                            logger.LogInformation("User signed in: {Email} (returning user)", email);
                        }
                        else
                        {
                            // Create new user
                            var newUser = UserEntity.Create(email, name ?? email, picture);
                            await userRepo.UpsertAsync(newUser);
                            logger.LogInformation("User signed in: {Email} (new user)", email);
                        }
                    }
                };
            });
    }
    else
    {
        Log.Warning("Google OAuth not configured - authentication will use dev login only");
    }

    builder.Services.AddAuthorization();

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
    }
    else
    {
        // Use stub service when OpenAI is not configured
        builder.Services.AddSingleton<IMealAnalysisService, StubMealAnalysisService>();
        Log.Warning("Azure OpenAI not configured - meal scanning will return mock data");
    }

    // Exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Configure forwarded headers for running behind reverse proxy (Azure Container Apps)
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    var app = builder.Build();

    // Use forwarded headers (must be first in pipeline)
    app.UseForwardedHeaders();

    // Initialize Table Storage (create tables if not exist)
    using (var scope = app.Services.CreateScope())
    {
        var dailyLogRepo = scope.ServiceProvider.GetRequiredService<IDailyLogRepository>() as DailyLogRepository;
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>() as UserRepository;

        await Task.WhenAll(
            dailyLogRepo?.InitializeAsync() ?? Task.CompletedTask,
            userRepo?.InitializeAsync() ?? Task.CompletedTask
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

    // Map Aspire default endpoints (health checks at /health and /alive)
    app.MapDefaultEndpoints();

    // Map feature endpoints
    app.MapAuthEndpoints();
    app.MapDailyLogEndpoints();
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
