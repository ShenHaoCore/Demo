using System.Collections.Concurrent;
using Demo.OptimisticLock.Api.Dtos;
using Demo.OptimisticLock.Api.Entities;

namespace Demo.OptimisticLock.Api.Application;

/// <summary>商品应用服务。</summary>
public sealed class ProductAppService : IProductAppService
{
    private readonly ConcurrentDictionary<int, Product> _products = new();
    private int _nextId = 2;

    public ProductAppService()
    {
        _products[1] = new Product(1, "乐观锁演示商品", 100, 1);
        _products[2] = new Product(2, "另一件商品", 50, 1);
    }

    public Task<ProductDto?> GetAsync(int id)
    {
        if (!_products.TryGetValue(id, out var entity))
        {
            return Task.FromResult<ProductDto?>(null);
        }

        return Task.FromResult<ProductDto?>(MapToDto(entity));
    }

    public Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var id = Interlocked.Increment(ref _nextId);
        var entity = new Product(id, input.Name.Trim(), input.Stock, 1);
        _products[id] = entity;
        return Task.FromResult(MapToDto(entity));
    }

    public Task<UpdateResult> UpdateStockAsync(int id, UpdateStockDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        while (true)
        {
            if (!_products.TryGetValue(id, out var current))
            {
                return Task.FromResult(new UpdateResult { Status = UpdateStatus.NotFound });
            }

            if (current.Version != input.ExpectedVersion)
            {
                return Task.FromResult(new UpdateResult
                {
                    Status = UpdateStatus.Conflict,
                    Product = MapToDto(current)
                });
            }

            var updated = new Product(current.Id, current.Name, input.Stock, current.Version + 1);

            if (_products.TryUpdate(id, updated, current))
            {
                return Task.FromResult(new UpdateResult
                {
                    Status = UpdateStatus.Success,
                    Product = MapToDto(updated)
                });
            }
            // 并发下字典值已变，重试读取
        }
    }

    private static ProductDto MapToDto(Product entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Stock = entity.Stock,
        Version = entity.Version
    };
}
