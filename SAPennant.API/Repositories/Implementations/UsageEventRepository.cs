using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Domain;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Base;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Repositories.Implementations;

public class UsageEventRepository : EfRepository<UsageEvent>, IUsageEventRepository
{
    public UsageEventRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UsageSummary> GetSummaryAsync(DateOnly from, DateOnly? previousFrom, string[] excludeVisitors)
    {
        var all = _dbSet.Where(e => e.VisitorId == null || !excludeVisitors.Contains(e.VisitorId));
        var window = all.Where(e => e.Day >= from);

        var days = await window
            .GroupBy(e => e.Day)
            .Select(g => new
            {
                Day = g.Key,
                Visits = g.Count(e => e.Event == "visit"),
                Visitors = g.Select(e => e.VisitorId).Distinct().Count(),
                Events = g.Count(),
            })
            .OrderBy(d => d.Day)
            .ToListAsync();

        var byEvent = await window
            .GroupBy(e => e.Event)
            .Select(g => new { Event = g.Key, Count = g.Count() })
            .ToListAsync();

        var byHour = new int[24];
        foreach (var h in await window.Where(e => e.Event == "visit").GroupBy(e => e.Hour)
                     .Select(g => new { Hour = g.Key, Count = g.Count() }).ToListAsync())
            if (h.Hour is >= 0 and < 24) byHour[h.Hour] = h.Count;

        var byWeekday = new int[7]; // Monday first
        foreach (var d in days) byWeekday[((int)d.Day.DayOfWeek + 6) % 7] += d.Visits;

        // Details are grouped raw in SQL, then collapsed in memory (a search
        // term logs with its result count, which changes between syncs).
        var details = await window
            .Where(e => e.Event == "search" || e.Event == "player" || e.Event == "club"
                     || (e.Event == "visit" && e.Detail.Contains(" ← ")))
            .GroupBy(e => new { e.Event, e.Detail })
            .Select(g => new { g.Key.Event, g.Key.Detail, Count = g.Count() })
            .ToListAsync();

        List<UsageCount> Top(string eventName, Func<string, bool>? only = null, Func<string, string?>? key = null) => details
            .Where(d => d.Event == eventName && (only == null || only(d.Detail)))
            .Select(d => new { Name = key != null ? key(d.Detail) : UsageEvents.NormaliseDetail(eventName, d.Detail), d.Count })
            .Where(d => !string.IsNullOrEmpty(d.Name))
            .GroupBy(d => d.Name!)
            .Select(g => new UsageCount(g.Key, g.Sum(d => d.Count)))
            .OrderByDescending(c => c.Count).ThenBy(c => c.Name)
            .Take(10)
            .ToList();

        var newVisitors = await all
            .Where(e => e.VisitorId != null)
            .GroupBy(e => e.VisitorId)
            .Where(g => g.Min(e => e.Day) >= from)
            .CountAsync();

        UsageTotals? previous = null;
        if (previousFrom != null)
        {
            var prev = all.Where(e => e.Day >= previousFrom && e.Day < from);
            previous = new UsageTotals(
                await prev.CountAsync(e => e.Event == "visit"),
                await prev.Select(e => e.VisitorId).Distinct().CountAsync(),
                await prev.CountAsync());
        }

        return new UsageSummary(
            Visits: days.Sum(d => d.Visits),
            Visitors: await window.Select(e => e.VisitorId).Distinct().CountAsync(),
            Events: days.Sum(d => d.Events),
            NewVisitors: newVisitors,
            MobileVisits: await window.CountAsync(e => e.Event == "visit" && e.IsMobile == true),
            KnownDeviceVisits: await window.CountAsync(e => e.Event == "visit" && e.IsMobile != null),
            Previous: previous,
            Days: days.Select(d => new UsageDay(d.Day, d.Visits, d.Visitors, d.Events)).ToList(),
            ByHour: byHour,
            ByWeekday: byWeekday,
            ByEvent: byEvent.OrderByDescending(e => e.Count).Select(e => new UsageCount(e.Event, e.Count)).ToList(),
            TopSearches: Top("search"),
            TopMisses: Top("search", only: UsageEvents.IsSearchMiss),
            TopPlayers: Top("player"),
            TopClubs: Top("club"),
            TopReferrers: Top("visit", key: UsageEvents.Referrer));
    }
}
