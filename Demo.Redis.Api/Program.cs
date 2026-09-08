using System.Collections.Concurrent;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ProductDb>();
builder.Services.AddSingleton<MemoryCacheAside>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.Redis.Api" }));

app.MapGet("/api/products/{id:int}", (int id, ProductDb db, MemoryCacheAside cache) =>
{
    var cacheKey = ProductKey(id);
    if (cache.TryGet(cacheKey, out var cached))
    {
        return Results.Ok(new
        {
            source = "cache",
            cacheHit = true,
            product = cached
        });
    }

    var product = db.Get(id);
    if (product is null)
    {
        return Results.NotFound(new { message = "商品不存在", id, cacheHit = false, source = "db" });
    }

    cache.Set(cacheKey, product);
    return Results.Ok(new
    {
        source = "db",
        cacheHit = false,
        message = "缓存未命中，已从模拟 DB 读取并回填缓存",
        product
    });
})
.WithName("GetProduct")
.WithSummary("Cache-Aside：先查缓存，未命中读 DB 再回填");

app.MapPut("/api/products/{id:int}", (int id, UpdateProductRequest request, ProductDb db, MemoryCacheAside cache) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Name) || request.Price < 0)
    {
        return Results.BadRequest(new { message = "请提供有效的 name 与 price（>=0）" });
    }

    var updated = db.Upsert(id, request.Name.Trim(), request.Price);
    cache.Remove(ProductKey(id));

    return Results.Ok(new
    {
        message = "已更新模拟 DB，并删除对应缓存（Cache-Aside 写策略）",
        product = updated,
        cacheKeyRemoved = ProductKey(id)
    });
})
.WithName("UpdateProduct")
.WithSummary("更新 DB 并删除缓存");

app.MapGet("/api/cache/stats", (MemoryCacheAside cache) =>
{
    var stats = cache.GetStats();
    return Results.Ok(new
    {
        hits = stats.Hits,
        misses = stats.Misses,
        entries = stats.Entries,
        message = "内存模拟缓存命中统计"
    });
})
.WithName("GetCacheStats")
.WithSummary("查看缓存命中/未命中统计");

app.Run();

static string ProductKey(int id) => $"product:{id}";

sealed class ProductDb
{
    private readonly ConcurrentDictionary<int, Product> _db = new();

    public ProductDb()
    {
        _db[1] = new Product(1, "演示商品 A", 19.9m);
        _db[2] = new Product(2, "演示商品 B", 39.5m);
        _db[3] = new Product(3, "演示商品 C", 99.0m);
    }

    public Product? Get(int id) => _db.TryGetValue(id, out var p) ? p : null;

    public Product Upsert(int id, string name, decimal price)
    {
        var product = new Product(id, name, price);
        _db[id] = product;
        return product;
    }
}

sealed class MemoryCacheAside
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

    public CacheStats GetStats() => new(
        Interlocked.Read(ref _hits),
        Interlocked.Read(ref _misses),
        _cache.Count);
}

record Product(int Id, string Name, decimal Price);

record UpdateProductRequest(string Name, decimal Price);

record CacheStats(long Hits, long Misses, int Entries);
