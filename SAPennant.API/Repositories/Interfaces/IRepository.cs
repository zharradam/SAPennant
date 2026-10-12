namespace SAPennant.API.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    Task<int> SaveChangesAsync();
}