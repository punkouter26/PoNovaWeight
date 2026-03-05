using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Infrastructure;

/// <summary>
/// Initializes Azure Table Storage tables as a hosted background service so the
/// app can start responding (health checks, warm-up probes) immediately without
/// waiting for network-dependent table creation to complete.
/// </summary>
public sealed class BackgroundTableInitService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BackgroundTableInitService> _logger;

    public BackgroundTableInitService(IServiceProvider services, ILogger<BackgroundTableInitService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Table Storage initialization starting in background…");

        try
        {
            await using var scope = _services.CreateAsyncScope();

            var dailyLogRepo = scope.ServiceProvider.GetRequiredService<IDailyLogRepository>() as DailyLogRepository;
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>() as UserRepository;
            var settingsRepo = scope.ServiceProvider.GetRequiredService<IUserSettingsRepository>() as UserSettingsRepository;

            await Task.WhenAll(
                dailyLogRepo?.InitializeAsync() ?? Task.CompletedTask,
                userRepo?.InitializeAsync() ?? Task.CompletedTask,
                settingsRepo?.InitializeAsync() ?? Task.CompletedTask
            );

            _logger.LogInformation("Table Storage initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table Storage initialization failed. The app is still running but table operations may fail until tables are created.");
        }
    }
}
