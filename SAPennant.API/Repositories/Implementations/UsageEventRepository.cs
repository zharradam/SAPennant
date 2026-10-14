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

    public async Task<UsageSummary> GetSummaryAsync(DateOnly from)
    {
        var window = _dbSet.Where(e => e.Day >= from);

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

        // Details are grouped raw in SQL, then collapsed in memory (a search
        // term logs with its result count, which changes between syncs).
        var details = await window
            .Where(e => e.Event == "search" || e.Event == "player" || e.Event == "club")
            .GroupBy(e => new { e.Event, e.Detail })
            .Select(g => new { g.Key.Event, g.Key.Detail, Count = g.Count() })
            .ToListAsync();

        List<UsageCount> Top(string eventName) => details
            .Where(d => d.Event == eventName)
            .GroupBy(d => UsageEvents.NormaliseDetail(eventName, d.Detail))
            .Where(g => g.Key != "")
            .Select(g => new UsageCount(g.Key, g.Sum(d => d.Count)))
            .OrderByDescending(c => c.Count).ThenBy(c => c.Name)
            .Take(10)
            .ToList();

        return new UsageSummary(
            Visits: days.Sum(d => d.Visits),
            Visitors: await window.Select(e => e.VisitorId).Distinct().CountAsync(),
            Events: days.Sum(d => d.Events),
            Days: days.Select(d => new UsageDay(d.Day, d.Visits, d.Visitors, d.Events)).ToList(),
            ByEvent: byEvent.OrderByDescending(e => e.Count).Select(e => new UsageCount(e.Event, e.Count)).ToList(),
            TopSearches: Top("search"),
            TopPlayers: Top("player"),
            TopClubs: Top("club"));
    }
}
