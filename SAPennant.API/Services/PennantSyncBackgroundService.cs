using SAPennant.API.Services;

namespace SAPennant.API.Services;

public class PennantSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PennantSyncBackgroundService> _logger;

    public PennantSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PennantSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pennant sync background service started.");
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        var lastSync = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<SettingsService>();
            var isEnabled = await settings.GetBoolAsync("AutoSyncEnabled", true);
            var intervalMinutes = await settings.GetIntAsync("PollingIntervalMinutes", 60);
            var nextSync = lastSync.AddMinutes(intervalMinutes);

            if (isEnabled && DateTime.UtcNow >= nextSync)
            {
                try
                {
                    var syncService = scope.ServiceProvider.GetRequiredService<GolfboxSyncService>();
                    await syncService.SyncCurrentYearUnsettledAsync();
                    lastSync = DateTime.UtcNow;
                    _logger.LogInformation("Sync complete. Next sync in {Minutes} minutes.", intervalMinutes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during background sync.");
                }
            }
            else if (!isEnabled)
            {
                _logger.LogInformation("Background sync is disabled, skipping.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}