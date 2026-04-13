using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Base;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Repositories.Implementations;

public class PennantMatchRepository : EfRepository<PennantMatch>, IPennantMatchRepository
{
    public PennantMatchRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PennantMatch>> GetByYearAsync(int year)
    {
        return await _dbSet.Where(m => m.Year == year).ToListAsync();
    }

    public async Task<IEnumerable<PennantMatch>> GetByYearAndPoolAsync(int year, string pool)
    {
        return await _dbSet
            .Where(m => m.Year == year && m.Pool == pool)
            .ToListAsync();
    }

    public async Task<IEnumerable<PennantMatch>> GetByYearAndPoolAndRoundAsync(
        int year, string pool, string round)
    {
        return await _dbSet
            .Where(m => m.Year == year && m.Pool == pool && m.Round == round)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int year, bool isFinals, bool isSenior)
    {
        return await _dbSet.AnyAsync(m =>
            m.Year == year &&
            m.IsFinals == isFinals &&
            m.IsSenior == isSenior);
    }

    public async Task<IEnumerable<string>> GetDistinctYearsAsync()
    {
        return await _dbSet
            .Select(m => m.Year.ToString())
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetDistinctPoolsAsync(int? year = null)
    {
        var query = _dbSet.AsQueryable();
        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);
        return await query
            .Select(m => m.Pool)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
    }

    public async Task DeleteByYearAsync(int year)
    {
        var matches = await _dbSet.Where(m => m.Year == year).ToListAsync();
        _dbSet.RemoveRange(matches);
    }

    public async Task DeleteByYearPoolRoundAsync(
        int year, string pool, string round, bool isSenior)
    {
        var matches = await _dbSet
            .Where(m => m.Year == year &&
                        m.Pool == pool &&
                        m.Round == round &&
                        m.IsSenior == isSenior)
            .ToListAsync();
        _dbSet.RemoveRange(matches);
    }

    public async Task<IEnumerable<PennantMatch>> SearchByPlayerNameAsync(string query)
    {
        return await _dbSet
            .Where(m => EF.Functions.Like(m.PlayerName.ToLower(), $"%{query.ToLower()}%"))
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetPlayerSuggestionsAsync(string query)
    {
        var parts = query.Trim().ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstPart = parts[0];

        var names = await _dbSet
            .Where(m =>
                (EF.Functions.Like(m.PlayerName.ToLower(), $"{firstPart}%") ||
                 EF.Functions.Like(m.PlayerName.ToLower(), $"% {firstPart}%")) &&
                !m.PlayerName.StartsWith("-") &&
                m.PlayerName.Length > 3)
            .Select(m => m.PlayerName)
            .Distinct()
            .ToListAsync();

        return names
            .Where(n => parts.All(part =>
                n.Split(' ').Any(word =>
                    word.StartsWith(part, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(n => n)
            .Take(10);
    }

    public async Task<IEnumerable<PennantMatch>> GetLeaderboardDataAsync(
        int? year, string? division, string? pool, bool? isSenior)
    {
        var query = _dbSet.AsQueryable();

        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);
        if (!string.IsNullOrEmpty(pool))
            query = query.Where(m => m.Pool == pool);
        if (isSenior.HasValue)
            query = query.Where(m => m.IsSenior == isSenior.Value);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<(string Pool, bool IsSenior)>> GetPoolDivisionsAsync(int? year)
    {
        var query = _dbSet.AsQueryable();
        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);

        return await query
            .Select(m => new { m.Pool, m.IsSenior })
            .Distinct()
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x => (x.Pool, x.IsSenior)));
    }

    public async Task<IEnumerable<string>> GetClubSuggestionsAsync(string query)
    {
        return await _dbSet
            .Where(m =>
                m.PlayerClub != null &&
                m.PlayerClub.Length > 0 &&
                EF.Functions.Like(m.PlayerClub.ToLower(), $"%{query.Trim().ToLower()}%"))
            .Select(m => m.PlayerClub!)
            .Distinct()
            .OrderBy(c => c)
            .Take(15)
            .ToListAsync();
    }

    public async Task<IEnumerable<PennantMatch>> GetByClubAsync(string clubName)
    {
        return await _dbSet
            .Where(m =>
                m.PlayerClub == clubName &&
                !string.IsNullOrWhiteSpace(m.PlayerName) &&
                !m.PlayerName.StartsWith("-") &&
                m.PlayerName.Length > 3)
            .ToListAsync();
    }

    public async Task<IEnumerable<PennantMatch>> GetHandicapDataAsync()
    {
        return await _dbSet
            .Where(m =>
                m.Format == "single" &&
                m.PlayerHandicap != null &&
                m.PlayerHandicap != "")
            .ToListAsync();
    }

    public async Task<IEnumerable<PennantMatch>> GetHandicapHistoryAsync(string playerName)
    {
        return await _dbSet
            .Where(m =>
                m.PlayerName == playerName &&
                m.Format == "single" &&
                m.PlayerHandicap != null &&
                m.PlayerHandicap != "")
            .ToListAsync();
    }

    public async Task<IEnumerable<PennantMatch>> GetTeamMatchesAsync(int year, string pool, bool includeFinals = false)
    {
        return await _dbSet
            .Where(m => m.Year == year && m.Pool == pool && (includeFinals || !m.IsFinals))
            .ToListAsync();
    }

    public async Task<IEnumerable<PennantMatch>> GetMatchAsync(int year, string pool, string round, string home, string away)
    {
        return await _dbSet
            .Where(m => m.Year == year && m.Pool == pool && m.Round == round
                     && m.HomeClub == home && m.AwayClub == away)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetRoundsListAsync(int year, string pool)
    {
        return await _dbSet
            .Where(m => m.Year == year && m.Pool == pool)
            .Select(m => m.Round)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IEnumerable<PennantMatch>> GetClubMatchesAsync(int year, string pool, string club)
    {
        return await _dbSet
            .Where(m => m.Year == year && m.Pool == pool
                     && (m.HomeClub == club || m.AwayClub == club))
            .ToListAsync();
    }
}