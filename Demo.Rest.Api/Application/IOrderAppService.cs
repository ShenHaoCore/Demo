using Demo.Rest.Api.Dtos;

namespace Demo.Rest.Api.Application;

/// <summary>
/// 订单应用服务
/// </summary>
public interface IOrderAppService
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<List<OrderDto>> GetListAsync();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<OrderDto?> GetAsync(Guid id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<OrderDto> CreateAsync(CreateOrderDto input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<OrderDto?> UpdateAsync(Guid id, UpdateOrderDto input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<OrderDto?> PatchAsync(Guid id, PatchOrderDto input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(Guid id);
}
