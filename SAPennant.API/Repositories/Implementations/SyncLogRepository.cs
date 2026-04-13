using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Base;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Repositories.Implementations;

public class SyncLogRepository : EfRepository<SyncLog>, ISyncLogRepository
{
    public SyncLogRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SyncLog?> GetLatestAsync()
    {
        return await _dbSet
            .OrderByDescending(s => s.SyncedAt)
            .FirstOrDefaultAsync();
    }
}