using System.Collections.Concurrent;
using Demo.Versioning.Api.Dtos;
using Demo.Versioning.Api.Entities;

namespace Demo.Versioning.Api.Application;

/// <summary>产品应用服务。</summary>
public sealed class ProductAppService : IProductAppService
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public ProductAppService()
    {
        _products[Guid.Parse("11111111-1111-1111-1111-111111111111")] = new Product(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "笔记本",
            5999m,
            "轻薄办公本，适合日常开发");
        _products[Guid.Parse("22222222-2222-2222-2222-222222222222")] = new Product(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "机械键盘",
            399m,
            "青轴，可热插拔");
    }

    public Task<List<ProductDto>> GetListAsync(bool includeDescription)
    {
        var list = _products.Values
            .OrderBy(p => p.Name)
            .Select(p => MapToDto(p, includeDescription))
            .ToList();
        return Task.FromResult(list);
    }

    public Task<ProductDto?> GetAsync(Guid id, bool includeDescription)
    {
        if (!_products.TryGetValue(id, out var entity))
        {
            return Task.FromResult<ProductDto?>(null);
        }

        return Task.FromResult<ProductDto?>(MapToDto(entity, includeDescription));
    }

    public Task<ProductDto> CreateAsync(CreateProductDto input, bool includeDescription)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateCreate(input);

        // V1 忽略客户端传入的 Description
        var description = includeDescription && !string.IsNullOrWhiteSpace(input.Description)
            ? input.Description.Trim()
            : string.Empty;

        var entity = new Product(Guid.NewGuid(), input.Name.Trim(), input.Price, description);
        _products[entity.Id] = entity;
        return Task.FromResult(MapToDto(entity, includeDescription));
    }

    private static void ValidateCreate(CreateProductDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new ArgumentException("名称不能为空", nameof(input.Name));
        }

        if (input.Price <= 0)
        {
            throw new ArgumentException("价格必须大于 0", nameof(input.Price));
        }
    }

    private static ProductDto MapToDto(Product entity, bool includeDescription) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Price = entity.Price,
        Description = includeDescription ? entity.Description : null
    };
}
