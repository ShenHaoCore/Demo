using Demo.Idempotent.Api.Dtos;

namespace Demo.Idempotent.Api.Application;

/// <summary>创建订单结果。</summary>
public abstract record CreateOrderOutcome;

public sealed record CreateOrderSuccess(OrderDto Order, string Location) : CreateOrderOutcome;

public sealed record CreateOrderReplay(IdempotencyRecord Record) : CreateOrderOutcome;

public sealed record CreateOrderConflict(string Message) : CreateOrderOutcome;

public sealed record CreateOrderInvalid(string Message) : CreateOrderOutcome;

/// <summary>订单应用服务契约。</summary>
public interface IOrderAppService
{
    /// <summary>唯一写路径：校验 + 同事务写入订单与幂等快照（或回放/冲突）。</summary>
    Task<CreateOrderOutcome> CreateAsync(
        CreateOrderDto input,
        string idempotencyKey,
        string bodyHash,
        CancellationToken cancellationToken = default);
}

