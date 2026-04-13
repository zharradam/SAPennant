using SAPennant.API.Models;

namespace SAPennant.API.Repositories.Interfaces;

public interface ISyncLogRepository : IRepository<SyncLog>
{
    Task<SyncLog?> GetLatestAsync();
}