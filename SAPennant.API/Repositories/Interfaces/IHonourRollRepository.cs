using SAPennant.API.Models;

namespace SAPennant.API.Repositories.Interfaces;

public interface IHonourRollRepository : IRepository<HonourRoll>
{
    Task<IEnumerable<HonourRoll>> GetAsync(string? competition, string? pool, int? year, string? club);
    Task<IEnumerable<string>> GetCompetitionsAsync();
    Task<IEnumerable<string>> GetPoolsAsync(string? competition);
    Task<IEnumerable<string>> GetClubsAsync();
    Task<IEnumerable<HonourRollNarrative>> GetNarrativesAsync();
}