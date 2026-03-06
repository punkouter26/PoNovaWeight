using System.Security.Claims;
using System.Threading.RateLimiting;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.Validation;
using Serilog;

namespace PoNovaWeight.Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void ConfigureNovaKeyVault(this WebApplicationBuilder builder)
    {
        // Configuration priority (highest wins):
        // 1. Environment variables
        // 2. User secrets (development only)
        // 3. Azure Key Vault (non-development)
        // 4. appsettings.{Environment}.json
        // 5. appsettings.json
        var keyVaultUri = builder.Configuration["KeyVault:VaultUri"]
            ?? builder.Configuration["KeyVault:Url"];

        if (!string.IsNullOrEmpty(keyVaultUri) && !builder.Environment.IsDevelopment())
        {
            try
            {
                // In Production (Azure App Service with Managed Identity), use
                // ManagedIdentityCredential directly to avoid DefaultAzureCredential
                // fallback delays during startup probes.
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
    }

    public static IServiceCollection AddNovaApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();
        services.AddApplicationInsightsTelemetry();

        services.AddDistributedMemoryCache();
        services.AddMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("api", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetRateLimitPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        services.AddOutputCache(options =>
        {
            options.AddPolicy("ShortCache", policy => policy
                .Expire(TimeSpan.FromMinutes(5))
                .SetVaryByHeader("Cookie")
                .SetVaryByHeader("Authorization"));

            options.AddPolicy("TrendsCache", policy => policy
                .Expire(TimeSpan.FromMinutes(5))
                .SetVaryByQuery("days")
                .SetVaryByHeader("Cookie")
                .SetVaryByHeader("Authorization"));
        });

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Program>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<DailyLogDtoValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddHealthChecks();

        var tableStorageValue = configuration.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:AzureStorage is required. Configure via Key Vault or appsettings.");

        var tableServiceClient = Uri.TryCreate(tableStorageValue, UriKind.Absolute, out var tableEndpoint)
            && tableEndpoint.Scheme is "https" or "http"
            ? new TableServiceClient(tableEndpoint, new ManagedIdentityCredential())
            : new TableServiceClient(tableStorageValue);

        services.AddSingleton(tableServiceClient);
        services.AddSingleton<IDailyLogRepository, DailyLogRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IUserSettingsRepository, UserSettingsRepository>();
        services.AddHostedService<BackgroundTableInitService>();

        services.AddNovaAuthentication(configuration, environment);

        var openAiEndpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required. Configure via Key Vault or appsettings.");
        var openAiApiKey = configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required. Configure via Key Vault or appsettings.");

        services.AddSingleton(new AzureOpenAIClient(
            new Uri(openAiEndpoint),
            new Azure.AzureKeyCredential(openAiApiKey)));
        services.AddSingleton<IMealAnalysisService, MealAnalysisService>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    private static string GetRateLimitPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.Email)
            ?? httpContext.User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(remoteIp) ? "anon" : $"ip:{remoteIp}";
    }
}