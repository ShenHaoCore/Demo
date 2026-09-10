namespace Demo.Idempotent.Api.Dtos;

/// <summary>订单输出 DTO。</summary>
public sealed class OrderDto
{
    public Guid Id { get; set; }
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
