using Demo.OrderTimeout.Api.Entities;

namespace Demo.OrderTimeout.Api.Dtos;

public enum TransitionStatus
{
    Success,
    Conflict,
    NotFound
}

/// <summary>创建订单输入。</summary>
public record CreateOrderDto(string Product, decimal Amount, int? TimeoutSeconds);

/// <summary>状态流转结果。</summary>
public record TransitionResult(TransitionStatus Status, Order? Order);
