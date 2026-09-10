using Demo.Idempotent.Api.Application.Idempotency;

namespace Demo.Idempotent.Api.Application.Orders;

/// <summary>回放已完成快照。</summary>
public sealed record CreateOrderReplayResult(IdempotencySnapshot Snapshot) : CreateOrderResult;
