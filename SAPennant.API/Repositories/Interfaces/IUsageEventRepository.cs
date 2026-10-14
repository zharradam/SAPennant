using SAPennant.API.Models;

namespace SAPennant.API.Repositories.Interfaces;

public record UsageDay(DateOnly Day, int Visits, int Visitors, int Events);
public record UsageCount(string Name, int Count);
public record UsageSummary(
    int Visits, int Visitors, int Events,
    List<UsageDay> Days,
    List<UsageCount> ByEvent,
    List<UsageCount> TopSearches,
    List<UsageCount> TopPlayers,
    List<UsageCount> TopClubs);

public interface IUsageEventRepository : IRepository<UsageEvent>
{
    /// Summary of every event on or after `from` (Adelaide day).
    Task<UsageSummary> GetSummaryAsync(DateOnly from);
}
