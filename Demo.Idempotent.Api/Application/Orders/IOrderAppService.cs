using Demo.Idempotent.Api.Dtos;

namespace Demo.Idempotent.Api.Application.Orders;

/// <summary>订单应用服务契约。</summary>
public interface IOrderAppService
{
    /// <summary>唯一写路径：校验 + 同事务写入订单与幂等快照（或回放/冲突）。</summary>
    Task<CreateOrderResult> CreateAsync(
        CreateOrderDto input,
        string idempotencyKey,
        string bodyHash,
        CancellationToken cancellationToken = default);
}
