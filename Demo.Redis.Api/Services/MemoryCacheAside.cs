using System.Collections.Concurrent;
using Demo.Redis.Api.Dtos;
using Demo.Redis.Api.Entities;

namespace Demo.Redis.Api.Services;

/// <summary>内存模拟 Cache-Aside 缓存组件。</summary>
public sealed class MemoryCacheAside
{
    private readonly ConcurrentDictionary<string, Product> _cache = new(StringComparer.Ordinal);
    private long _hits;
    private long _misses;

    public bool TryGet(string key, out Product? product)
    {
        if (_cache.TryGetValue(key, out product))
        {
            Interlocked.Increment(ref _hits);
            return true;
        }

        Interlocked.Increment(ref _misses);
        product = null;
        return false;
    }

    public void Set(string key, Product product) => _cache[key] = product;

    public void Remove(string key) => _cache.TryRemove(key, out _);

    public CacheStatsDto GetStats() => new()
    {
        Hits = Interlocked.Read(ref _hits),
        Misses = Interlocked.Read(ref _misses),
        Entries = _cache.Count
    };
}
