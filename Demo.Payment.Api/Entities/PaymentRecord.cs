namespace Demo.Payment.Api.Entities;

public enum PaymentStatus
{
    Pending,
    Paid
}

/// <summary>支付单实体（避免与命名空间 Payment 冲突）。</summary>
public record PaymentRecord(
    Guid Id,
    string OrderId,
    decimal Amount,
    PaymentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    string? LastNotifyId);
