using Demo.Redis.Api.Dtos;
using Demo.Redis.Api.Entities;
using Demo.Redis.Api.Services;

namespace Demo.Redis.Api.Application;

/// <summary>商品应用服务（Cache-Aside 旁路逻辑）。</summary>
public sealed class ProductAppService(ProductDb db, MemoryCacheAside cache) : IProductAppService
{
    public ProductGetResultDto? Get(int id)
    {
        var cacheKey = ProductKey(id);
        if (cache.TryGet(cacheKey, out var cached) && cached is not null)
        {
            return new ProductGetResultDto
            {
                Source = "cache",
                CacheHit = true,
                Product = MapToDto(cached)
            };
        }

        var product = db.Get(id);
        if (product is null)
        {
            return null;
        }

        cache.Set(cacheKey, product);
        return new ProductGetResultDto
        {
            Source = "db",
            CacheHit = false,
            Message = "缓存未命中，已从模拟 DB 读取并回填缓存",
            Product = MapToDto(product)
        };
    }

    public ProductUpdateResultDto Update(int id, UpdateProductDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name) || input.Price < 0)
        {
            throw new ArgumentException("请提供有效的 name 与 price（>=0）");
        }

        var updated = db.Upsert(id, input.Name.Trim(), input.Price);
        var cacheKey = ProductKey(id);
        cache.Remove(cacheKey);

        return new ProductUpdateResultDto
        {
            Message = "已更新模拟 DB，并删除对应缓存（Cache-Aside 写策略）",
            Product = MapToDto(updated),
            CacheKeyRemoved = cacheKey
        };
    }

    public CacheStatsResultDto GetCacheStats()
    {
        var stats = cache.GetStats();
        return new CacheStatsResultDto
        {
            Hits = stats.Hits,
            Misses = stats.Misses,
            Entries = stats.Entries,
            Message = "内存模拟缓存命中统计"
        };
    }

    private static string ProductKey(int id) => $"product:{id}";

    private static ProductDto MapToDto(Product entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Price = entity.Price
    };
}
