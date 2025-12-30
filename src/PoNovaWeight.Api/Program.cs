using Azure.AI.OpenAI;
using Azure.Data.Tables;
using Azure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using PoNovaWeight.Api.Features.Auth;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Features.Health;
using PoNovaWeight.Api.Features.MealScan;
using PoNovaWeight.Api.Features.WeeklySummary;
using PoNovaWeight.Api.Infrastructure;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.Validation;
using Serilog;
using System.Security.Claims;

// Configure Serilog before building the host
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

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
        builder.AddAzureTableClient("tables", settings =>
        {
            // Disable health checks due to bug in AspNetCore.HealthChecks.Azure.Data.Tables
            // that sends invalid OData filter ($filter=false) causing 400 errors
            settings.DisableHealthChecks = true;
            settings.DisableTracing = false;
        });
    }
    else
    {
        // Fallback for non-Aspire scenarios (tests, standalone)
        var connectionString = builder.Configuration.GetConnectionString("AzureStorage")
            ?? "UseDevelopmentStorage=true";
        builder.Services.AddSingleton(new TableServiceClient(connectionString));
    }

    // Register repositories using TableServiceClient
    builder.Services.AddSingleton<IDailyLogRepository, DailyLogRepository>();
    builder.Services.AddSingleton<IUserRepository, UserRepository>();

    // Google OAuth + Cookie Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "nova-session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            // Return 401 for API calls instead of redirecting
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
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

    builder.Services.AddAuthorization();

    // Azure OpenAI
    var openAiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
    var openAiApiKey = builder.Configuration["AzureOpenAI:ApiKey"];
    if (!string.IsNullOrEmpty(openAiEndpoint) && !string.IsNullOrEmpty(openAiApiKey) && !openAiApiKey.StartsWith("YOUR-"))
    {
        builder.Services.AddSingleton(new AzureOpenAIClient(
            new Uri(openAiEndpoint),
            new Azure.AzureKeyCredential(openAiApiKey)));
        builder.Services.AddSingleton<IMealAnalysisService, MealAnalysisService>();
    }
    else
    {
        // Register a stub service for development without OpenAI
        builder.Services.AddSingleton<IMealAnalysisService, StubMealAnalysisService>();
    }

    // Exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var app = builder.Build();

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
        app.MapOpenApi();
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

    // Map Aspire default endpoints (health checks, etc.)
    app.MapDefaultEndpoints();

    // Map endpoints
    app.MapHealthEndpoints();
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
