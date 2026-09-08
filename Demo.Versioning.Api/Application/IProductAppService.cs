using Demo.Versioning.Api.Dtos;

namespace Demo.Versioning.Api.Application;

/// <summary>产品应用服务契约。</summary>
public interface IProductAppService
{
    Task<List<ProductDto>> GetListAsync(bool includeDescription);

    Task<ProductDto?> GetAsync(Guid id, bool includeDescription);

    /// <exception cref="ArgumentException">输入校验失败。</exception>
    Task<ProductDto> CreateAsync(CreateProductDto input, bool includeDescription);
}
