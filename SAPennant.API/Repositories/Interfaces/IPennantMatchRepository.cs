using SAPennant.API.Models;

namespace SAPennant.API.Repositories.Interfaces;

public interface IPennantMatchRepository : IRepository<PennantMatch>
{
    Task<IEnumerable<PennantMatch>> GetByYearAsync(int year);
    Task<IEnumerable<PennantMatch>> GetByYearAndPoolAsync(int year, string pool);
    Task<IEnumerable<PennantMatch>> GetByYearAndPoolAndRoundAsync(int year, string pool, string round);
    Task<bool> ExistsAsync(int year, bool isFinals, bool isSenior);
    Task<IEnumerable<string>> GetDistinctYearsAsync();
    Task<IEnumerable<string>> GetDistinctPoolsAsync(int? year = null);
    Task DeleteByYearAsync(int year);
    Task DeleteByYearPoolRoundAsync(int year, string pool, string round, bool isSenior);
    Task DeleteFinalsByYearPoolAsync(int year, string pool, bool isSenior);
    Task<IEnumerable<PennantMatch>> SearchByPlayerNameAsync(string query);
    Task<IEnumerable<string>> GetPlayerSuggestionsAsync(string query);
    Task<IEnumerable<PennantMatch>> GetLeaderboardDataAsync(int? year, string? division, string? pool, bool? isSenior);
    Task<IEnumerable<(string Pool, bool IsSenior)>> GetPoolDivisionsAsync(int? year);
    Task<IEnumerable<string>> GetClubSuggestionsAsync(string query);
    Task<IEnumerable<PennantMatch>> GetByClubAsync(string clubName);
    Task<IEnumerable<PennantMatch>> GetHandicapDataAsync();
    Task<IEnumerable<PennantMatch>> GetHandicapHistoryAsync(string playerName);
    Task<IEnumerable<PennantMatch>> GetTeamMatchesAsync(int year, string pool, bool includeFinals = false);
    Task<IEnumerable<PennantMatch>> GetMatchAsync(int year, string pool, string round, string home, string away);
    Task<IEnumerable<string>> GetRoundsListAsync(int year, string pool);
    Task<IEnumerable<PennantMatch>> GetClubMatchesAsync(int year, string pool, string club);
}