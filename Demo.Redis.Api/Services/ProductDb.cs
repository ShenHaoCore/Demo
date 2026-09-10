using System.Collections.Concurrent;
using Demo.Redis.Api.Entities;

namespace Demo.Redis.Api.Services;

/// <summary>模拟商品数据库。</summary>
public sealed class ProductDb
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
