using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Base;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Repositories.Implementations;

public class RoundStatusRepository : EfRepository<RoundStatus>, IRoundStatusRepository
{
    public RoundStatusRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<RoundStatus?> GetAsync(int year, string pool, string round)
    {
        return await _dbSet.FirstOrDefaultAsync(r =>
            r.Year == year && r.Pool == pool && r.Round == round);
    }

    public async Task<IEnumerable<RoundStatus>> GetByYearAsync(int year)
    {
        return await _dbSet.Where(r => r.Year == year).ToListAsync();
    }

    public async Task<IEnumerable<RoundStatus>> GetUnsettledAsync(int year)
    {
        return await _dbSet
            .Where(r => r.Year == year && !r.IsSettled)
            .ToListAsync();
    }

    public async Task DeleteByYearAsync(int year)
    {
        var statuses = await _dbSet.Where(r => r.Year == year).ToListAsync();
        _dbSet.RemoveRange(statuses);
    }
}