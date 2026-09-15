namespace Demo.Rest.Api.Entities;

/// <summary>
/// 订单实体
/// </summary>
public sealed class Order
{
    /// <summary>
    /// 订单唯一标识
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 客户名称
    /// </summary>
    public required string CustomerName { get; init; }

    /// <summary>
    /// 订单金额
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// 订单状态
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// 订单创建时间
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// 订单更新时间
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
