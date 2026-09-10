namespace Demo.OrderTimeout.Api.Entities;

public enum OrderStatus
{
    Pending,
    Paid,
    Closed
}

/// <summary>待支付 / 超时关单订单实体。</summary>
public record Order(
    Guid Id,
    string Product,
    decimal Amount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpireAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ClosedAt,
    string? CloseReason);
