using Demo.Redis.Api.Dtos;

namespace Demo.Redis.Api.Application;

/// <summary>商品应用服务契约（含 Cache-Aside）。</summary>
public interface IProductAppService
{
    ProductGetResultDto? Get(int id);

    /// <exception cref="ArgumentException">输入校验失败。</exception>
    ProductUpdateResultDto Update(int id, UpdateProductDto input);

    CacheStatsResultDto GetCacheStats();
}
