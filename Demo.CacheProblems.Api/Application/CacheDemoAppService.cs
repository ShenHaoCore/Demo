using System.Collections.Concurrent;
using Demo.CacheProblems.Api.Dtos;
using Demo.CacheProblems.Api.Entities;

namespace Demo.CacheProblems.Api.Application;

/// <summary>缓存穿透 / 击穿 / 雪崩演示应用服务。</summary>
public sealed class CacheDemoAppService : ICacheDemoAppService
{
    private readonly ConcurrentDictionary<string, (CacheItem Item, DateTimeOffset ExpireAt)> _cache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, object> _db = new(StringComparer.Ordinal)
    {
        ["1"] = new { id = "1", name = "存在的商品" },
        ["demo"] = new { id = "demo", name = "演示商品" }
    };
    private readonly SemaphoreSlim _rebuildLock = new(1, 1);

    public Task<object> GetPenetrationAsync(string? id, bool? cacheNull)
    {
        var enableNullCache = cacheNull ?? true;
        var key = string.IsNullOrWhiteSpace(id) ? "unknown" : id.Trim();
        var cacheKey = $"pen:{key}";

        if (TryGetCache(cacheKey, out var cached))
        {
            return Task.FromResult<object>(new
            {
                scenario = "穿透",
                cacheHit = true,
                cacheNullEnabled = enableNullCache,
                exists = cached.Exists,
                data = cached.Data,
                explanation = enableNullCache
                    ? "命中空值/数据缓存，避免再次打到 DB"
                    : "已命中缓存"
            });
        }

        var entity = DbGet(key);
        if (entity is null)
        {
            if (enableNullCache)
            {
                SetCache(cacheKey, new CacheItem(Exists: false, Data: null), TimeSpan.FromSeconds(30));
                return Task.FromResult<object>(new
                {
                    scenario = "穿透",
                    cacheHit = false,
                    cacheNullEnabled = true,
                    exists = false,
                    data = (object?)null,
                    dbQueried = true,
                    explanation = "DB 无数据，已缓存空值（短 TTL），后续同 id 不会穿透到 DB。对比：?cacheNull=false 时不缓存空值。"
                });
            }

            return Task.FromResult<object>(new
            {
                scenario = "穿透",
                cacheHit = false,
                cacheNullEnabled = false,
                exists = false,
                data = (object?)null,
                dbQueried = true,
                explanation = "未启用空值缓存：每次未命中都会查询 DB，恶意大量不存在 id 会造成穿透。"
            });
        }

        SetCache(cacheKey, new CacheItem(Exists: true, Data: entity), TimeSpan.FromSeconds(60));
        return Task.FromResult<object>(new
        {
            scenario = "穿透",
            cacheHit = false,
            cacheNullEnabled = enableNullCache,
            exists = true,
            data = entity,
            dbQueried = true,
            explanation = "数据存在，已回填缓存"
        });
    }

    public async Task<BreakdownResultDto> GetBreakdownAsync(bool? useLock)
    {
        const string cacheKey = "hot:product";
        var enableLock = useLock ?? true;

        if (TryGetCache(cacheKey, out var cached) && cached.Exists)
        {
            return new BreakdownResultDto
            {
                Body = new
                {
                    scenario = "击穿",
                    cacheHit = true,
                    useLock = enableLock,
                    data = cached.Data,
                    explanation = "热点 key 未过期，直接命中缓存"
                }
            };
        }

        if (!enableLock)
        {
            var rebuilt = await RebuildHotAsync(simulateSlowDb: true);
            SetCache(cacheKey, new CacheItem(true, rebuilt), TimeSpan.FromSeconds(5));
            return new BreakdownResultDto
            {
                Body = new
                {
                    scenario = "击穿",
                    cacheHit = false,
                    useLock = false,
                    rebuiltByThisRequest = true,
                    data = rebuilt,
                    explanation = "无锁模式：热点过期时并发请求会同时打 DB。对比：默认 useLock=true 用互斥锁重建。"
                }
            };
        }

        var lockTaken = false;
        try
        {
            lockTaken = await _rebuildLock.WaitAsync(TimeSpan.FromSeconds(3));
            if (!lockTaken)
            {
                await Task.Delay(50);
                if (TryGetCache(cacheKey, out cached) && cached.Exists)
                {
                    return new BreakdownResultDto
                    {
                        Body = new
                        {
                            scenario = "击穿",
                            cacheHit = true,
                            useLock = true,
                            waitedForRebuild = true,
                            data = cached.Data,
                            explanation = "未拿到锁，等待后命中其他请求重建的缓存"
                        }
                    };
                }

                return new BreakdownResultDto
                {
                    IsConflict = true,
                    Body = new
                    {
                        scenario = "击穿",
                        message = "热点重建中，请稍后重试",
                        useLock = true
                    }
                };
            }

            if (TryGetCache(cacheKey, out cached) && cached.Exists)
            {
                return new BreakdownResultDto
                {
                    Body = new
                    {
                        scenario = "击穿",
                        cacheHit = true,
                        useLock = true,
                        data = cached.Data,
                        explanation = "拿到锁后发现缓存已被重建"
                    }
                };
            }

            var rebuilt = await RebuildHotAsync(simulateSlowDb: true);
            SetCache(cacheKey, new CacheItem(true, rebuilt), TimeSpan.FromSeconds(5));
            return new BreakdownResultDto
            {
                Body = new
                {
                    scenario = "击穿",
                    cacheHit = false,
                    useLock = true,
                    lockAcquired = true,
                    rebuiltByThisRequest = true,
                    data = rebuilt,
                    explanation = "热点过期时仅持锁请求重建 DB，其他请求等待或命中新缓存，避免击穿。"
                }
            };
        }
        finally
        {
            if (lockTaken)
            {
                _rebuildLock.Release();
            }
        }
    }

    public Task<object> GetAvalancheAsync(bool? jitter, int? baseSeconds)
    {
        var useJitter = jitter ?? true;
        var baseTtl = Math.Clamp(baseSeconds ?? 10, 1, 120);
        var random = Random.Shared.Next(0, Math.Max(1, baseTtl / 2 + 1));
        var ttlSeconds = useJitter ? baseTtl + random : baseTtl;

        var batch = new List<object>();
        for (var i = 1; i <= 5; i++)
        {
            var key = $"ava:item:{i}";
            var itemTtl = useJitter
                ? TimeSpan.FromSeconds(baseTtl + Random.Shared.Next(0, Math.Max(1, baseTtl / 2 + 1)))
                : TimeSpan.FromSeconds(baseTtl);

            var data = new { id = i, name = $"批量商品-{i}" };
            SetCache(key, new CacheItem(true, data), itemTtl);
            batch.Add(new
            {
                key,
                ttlSeconds = itemTtl.TotalSeconds,
                expiresAt = DateTimeOffset.UtcNow.Add(itemTtl)
            });
        }

        return Task.FromResult<object>(new
        {
            scenario = "雪崩",
            jitterEnabled = useJitter,
            baseTtlSeconds = baseTtl,
            sampleTtlSeconds = ttlSeconds,
            items = batch,
            explanation = useJitter
                ? "过期时间 = 基础 TTL + 随机抖动，避免大量 key 同一时刻失效导致雪崩。对比：?jitter=false 时所有 key 使用相同 TTL。"
                : "未加抖动：批量 key 将在同一时刻过期，易引发瞬时 DB 压力（雪崩）。建议开启 jitter=true。"
        });
    }

    public Task ExpireHotKeyAsync()
    {
        _cache.TryRemove("hot:product", out _);
        return Task.CompletedTask;
    }

    private bool TryGetCache(string key, out CacheItem item)
    {
        if (_cache.TryGetValue(key, out var entry) && entry.ExpireAt > DateTimeOffset.UtcNow)
        {
            item = entry.Item;
            return true;
        }

        _cache.TryRemove(key, out _);
        item = default!;
        return false;
    }

    private void SetCache(string key, CacheItem item, TimeSpan ttl) =>
        _cache[key] = (item, DateTimeOffset.UtcNow.Add(ttl));

    private object? DbGet(string id) => _db.TryGetValue(id, out var v) ? v : null;

    private static async Task<object> RebuildHotAsync(bool simulateSlowDb)
    {
        if (simulateSlowDb)
        {
            await Task.Delay(200);
        }

        return new { id = "hot", name = "热点商品", rebuiltAt = DateTimeOffset.UtcNow };
    }
}
