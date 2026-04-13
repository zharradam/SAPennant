using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Base;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Repositories.Implementations;

public class SeasonRepository : EfRepository<Season>, ISeasonRepository
{
    public SeasonRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Season?> GetByYearAsync(int year)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Year == year);
    }

    public async Task<IEnumerable<Season>> GetAllOrderedAsync()
    {
        return await _dbSet.OrderByDescending(s => s.Year).ToListAsync();
    }
}