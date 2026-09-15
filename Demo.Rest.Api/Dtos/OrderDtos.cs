using System.ComponentModel;

namespace Demo.Rest.Api.Dtos;

/// <summary>订单输出 DTO。</summary>
public sealed class OrderDto
{
    [Description("订单 Id")]
    public Guid Id { get; set; }

    [Description("客户名称")]
    public string CustomerName { get; set; } = string.Empty;

    [Description("订单金额")]
    public decimal Amount { get; set; }

    [Description("订单状态，如 Pending / Paid / Cancelled")]
    public string Status { get; set; } = string.Empty;

    [Description("创建时间（UTC）")]
    public DateTimeOffset CreatedAt { get; set; }

    [Description("最后更新时间（UTC）")]
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>创建订单输入 DTO。</summary>
public sealed class CreateOrderDto
{
    [Description("客户名称")]
    public string CustomerName { get; set; } = string.Empty;

    [Description("订单金额，须大于 0")]
    public decimal Amount { get; set; }

    [Description("可选初始状态；省略时由服务端赋默认值")]
    public string? Status { get; set; }
}

/// <summary>全量更新订单输入 DTO。</summary>
public sealed class UpdateOrderDto
{
    [Description("客户名称")]
    public string CustomerName { get; set; } = string.Empty;

    [Description("订单金额，须大于 0")]
    public decimal Amount { get; set; }

    [Description("订单状态")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>部分更新订单输入 DTO。</summary>
public sealed class PatchOrderDto
{
    [Description("客户名称；为 null 表示不修改")]
    public string? CustomerName { get; set; }

    [Description("订单金额；为 null 表示不修改")]
    public decimal? Amount { get; set; }

    [Description("订单状态；为 null 表示不修改")]
    public string? Status { get; set; }
}
