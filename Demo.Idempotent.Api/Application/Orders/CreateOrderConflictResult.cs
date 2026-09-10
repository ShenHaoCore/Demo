namespace Demo.Idempotent.Api.Application.Orders;

/// <summary>同 Key 冲突（Body 不同或并发）。</summary>
public sealed record CreateOrderConflictResult(string Message) : CreateOrderResult;
