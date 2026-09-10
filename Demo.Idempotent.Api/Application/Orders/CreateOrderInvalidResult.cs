namespace Demo.Idempotent.Api.Application.Orders;

/// <summary>输入不合法。</summary>
public sealed record CreateOrderInvalidResult(string Message) : CreateOrderResult;
