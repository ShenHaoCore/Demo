namespace Demo.Idempotent.Api.Dtos;

/// <summary>创建订单输入 DTO。</summary>
public sealed class CreateOrderDto
{
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
