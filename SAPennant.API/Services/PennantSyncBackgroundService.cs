using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Domain;

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

        await BackfillMatchDatesAsync(stoppingToken);

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

    /// One-time backfill of PennantMatch.MatchDate from the legacy display-string
    /// Date column. No-ops once every parseable row has been filled.
    private async Task BackfillMatchDatesAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pending = await db.PennantMatches
                .Where(m => m.MatchDate == null && m.Date != "")
                .ToListAsync(stoppingToken);

            if (pending.Count == 0) return;

            var filled = 0;
            foreach (var match in pending)
            {
                var parsed = PennantDates.ParseDisplayDate(match.Date);
                if (parsed != null)
                {
                    match.MatchDate = parsed;
                    filled++;
                }
            }

            await db.SaveChangesAsync(stoppingToken);
            _logger.LogInformation(
                "MatchDate backfill: filled {Filled} of {Pending} rows missing a date.",
                filled, pending.Count);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MatchDate backfill failed — will retry on next startup.");
        }
    }
}