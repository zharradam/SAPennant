using Microsoft.Extensions.Caching.Memory;

namespace SAPennant.API.Services;

/// Version-stamped cache for aggregated match data. Match data only changes
/// when a Golfbox sync runs, so sync completion bumps the version, which
/// orphans every previously cached entry (the memory cache evicts them via
/// TTL). The TTL also acts as a safety net if an invalidation is ever missed.
public class DataCacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    private readonly IMemoryCache _cache;
    private int _version;

    public DataCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        var versionedKey = $"data:v{Volatile.Read(ref _version)}:{key}";
        return _cache.GetOrCreateAsync(versionedKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl;
            return factory();
        });
    }

    public void Invalidate() => Interlocked.Increment(ref _version);
}
