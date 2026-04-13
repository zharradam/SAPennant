using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Base;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Repositories.Implementations;

public class HonourRollRepository : EfRepository<HonourRoll>, IHonourRollRepository
{
    public HonourRollRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<HonourRoll>> GetAsync(
        string? competition, string? pool, int? year, string? club)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(competition))
            query = query.Where(h => h.Competition == competition);
        if (!string.IsNullOrEmpty(pool))
            query = query.Where(h => h.Pool == pool);
        if (year.HasValue)
            query = query.Where(h => h.Year == year.Value);
        if (!string.IsNullOrEmpty(club))
            query = query.Where(h => h.Winner == club);

        return await query
            .OrderByDescending(h => h.Year)
            .ThenBy(h => h.Pool)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetCompetitionsAsync()
    {
        return await _dbSet
            .Select(h => h.Competition)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetPoolsAsync(string? competition)
    {
        var query = _dbSet.AsQueryable();
        if (!string.IsNullOrEmpty(competition))
            query = query.Where(h => h.Competition == competition);
        return await query
            .Select(h => h.Pool)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetClubsAsync()
    {
        return await _dbSet
            .Where(h => h.Winner != null)
            .Select(h => h.Winner!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<IEnumerable<HonourRollNarrative>> GetNarrativesAsync()
    {
        return await _context.HonourRollNarratives.ToListAsync();
    }
}