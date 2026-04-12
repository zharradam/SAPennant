using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Services;

namespace SAPennant.API.Services;

public class PennantSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PennantSyncBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public PennantSyncBackgroundService(IServiceScopeFactory scopeFactory, ILogger<PennantSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pennant sync background service started.");
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<SettingsService>();
            var isEnabled = settings.GetBool("AutoSyncEnabled", true);

            if (isEnabled)
            {
                try
                {
                    var syncService = scope.ServiceProvider.GetRequiredService<GolfboxSyncService>();
                    await syncService.SyncCurrentYearUnsettledAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during background sync.");
                }
            }
            else
            {
                _logger.LogInformation("Background sync is disabled, skipping.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}