using Demo.Rest.Api.Dtos;

namespace Demo.Rest.Api.Application;

/// <summary>订单应用服务契约。</summary>
public interface IOrderAppService
{
    Task<List<OrderDto>> GetListAsync();

    Task<OrderDto?> GetAsync(Guid id);

    /// <exception cref="ArgumentException">输入校验失败。</exception>
    Task<OrderDto> CreateAsync(CreateOrderDto input);

    /// <exception cref="ArgumentException">输入校验失败。</exception>
    Task<OrderDto?> UpdateAsync(Guid id, UpdateOrderDto input);

    /// <exception cref="ArgumentException">输入校验失败。</exception>
    Task<OrderDto?> PatchAsync(Guid id, PatchOrderDto input);

    Task<bool> DeleteAsync(Guid id);
}
