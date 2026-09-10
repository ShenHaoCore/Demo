namespace Demo.Idempotent.Api.Entities;

/// <summary>订单实体（与幂等记录同库同事务落库）。</summary>
public sealed class Order
{
    public Guid Id { get; set; }
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
