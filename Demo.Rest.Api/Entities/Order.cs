namespace Demo.Rest.Api.Entities;

/// <summary>订单实体（Domain）。</summary>
public sealed class Order
{
    public Guid Id { get; init; }
    public required string CustomerName { get; init; }
    public decimal Amount { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
