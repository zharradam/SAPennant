using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Repositories.Implementations;

public class PoolFinalistConfigRepository : IPoolFinalistConfigRepository
{
    private readonly AppDbContext _context;

    public PoolFinalistConfigRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PoolFinalistConfig?> GetAsync(string pool)
    {
        return await _context.PoolFinalistConfigs.FindAsync(pool);
    }

    public async Task<IEnumerable<PoolFinalistConfig>> GetAllAsync()
    {
        return await _context.PoolFinalistConfigs.ToListAsync();
    }
}