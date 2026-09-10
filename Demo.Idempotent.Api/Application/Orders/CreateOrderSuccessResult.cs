using Demo.Idempotent.Api.Dtos;

namespace Demo.Idempotent.Api.Application.Orders;

/// <summary>首次创建成功。</summary>
public sealed record CreateOrderSuccessResult(OrderDto Order, string Location) : CreateOrderResult;
