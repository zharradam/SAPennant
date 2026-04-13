using SAPennant.API.Models;

namespace SAPennant.API.Repositories.Interfaces;

public interface ISeasonRepository : IRepository<Season>
{
    Task<Season?> GetByYearAsync(int year);
    Task<IEnumerable<Season>> GetAllOrderedAsync();
}