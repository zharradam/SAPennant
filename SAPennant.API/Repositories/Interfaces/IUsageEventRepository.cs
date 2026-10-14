using SAPennant.API.Models;

namespace SAPennant.API.Repositories.Interfaces;

public record UsageDay(DateOnly Day, int Visits, int Visitors, int Events);
public record UsageCount(string Name, int Count);
public record UsageTotals(int Visits, int Visitors, int Events);
public record UsageSummary(
    int Visits, int Visitors, int Events,
    int NewVisitors,
    int MobileVisits, int KnownDeviceVisits,
    UsageTotals? Previous,
    List<UsageDay> Days,
    int[] ByHour,
    int[] ByWeekday,
    List<UsageCount> ByEvent,
    List<UsageCount> TopSearches,
    List<UsageCount> TopMisses,
    List<UsageCount> TopPlayers,
    List<UsageCount> TopClubs,
    List<UsageCount> TopReferrers);

public interface IUsageEventRepository : IRepository<UsageEvent>
{
    /// Summary of events on or after `from` (Adelaide day), ignoring the
    /// given visitors. `previousFrom` adds totals for the window that ended
    /// the day before `from`, for comparison.
    Task<UsageSummary> GetSummaryAsync(DateOnly from, DateOnly? previousFrom, string[] excludeVisitors);
}
