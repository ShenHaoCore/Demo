using Demo.OptimisticLock.Api.Dtos;

namespace Demo.OptimisticLock.Api.Application;

/// <summary>库存更新结果状态。</summary>
public enum UpdateStatus
{
    Success,
    Conflict,
    NotFound
}

/// <summary>库存更新结果。</summary>
public sealed class UpdateResult
{
    public UpdateStatus Status { get; init; }
    public ProductDto? Product { get; init; }
}

/// <summary>商品应用服务契约。</summary>
public interface IProductAppService
{
    Task<ProductDto?> GetAsync(int id);

    Task<ProductDto> CreateAsync(CreateProductDto input);

    Task<UpdateResult> UpdateStockAsync(int id, UpdateStockDto input);
}
