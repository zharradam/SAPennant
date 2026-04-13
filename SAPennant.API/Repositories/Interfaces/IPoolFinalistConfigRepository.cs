using SAPennant.API.Models;

namespace SAPennant.API.Repositories.Interfaces;

public interface IPoolFinalistConfigRepository
{
    Task<PoolFinalistConfig?> GetAsync(string pool);
    Task<IEnumerable<PoolFinalistConfig>> GetAllAsync();
}