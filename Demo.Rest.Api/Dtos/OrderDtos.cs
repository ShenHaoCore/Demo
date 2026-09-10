namespace Demo.Rest.Api.Dtos;

/// <summary>订单输出 DTO。</summary>
public sealed class OrderDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>创建订单输入 DTO。</summary>
public sealed class CreateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Status { get; set; }
}

/// <summary>全量更新订单输入 DTO。</summary>
public sealed class UpdateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>部分更新订单输入 DTO。</summary>
public sealed class PatchOrderDto
{
    public string? CustomerName { get; set; }
    public decimal? Amount { get; set; }
    public string? Status { get; set; }
}
