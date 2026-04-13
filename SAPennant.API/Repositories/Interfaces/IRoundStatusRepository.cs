using SAPennant.API.Models;

namespace SAPennant.API.Repositories.Interfaces;

public interface IRoundStatusRepository : IRepository<RoundStatus>
{
    Task<RoundStatus?> GetAsync(int year, string pool, string round);
    Task<IEnumerable<RoundStatus>> GetByYearAsync(int year);
    Task<IEnumerable<RoundStatus>> GetUnsettledAsync(int year);
    Task DeleteByYearAsync(int year);
}