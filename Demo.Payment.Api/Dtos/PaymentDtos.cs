using System.Text.Json;
using Demo.Payment.Api.Entities;

namespace Demo.Payment.Api.Dtos;

public enum NotifyKind
{
    Applied,
    Duplicate,
    AlreadyPaidOtherNotify,
    AmountMismatch,
    NotFound
}

/// <summary>创建支付单输入。</summary>
public record CreatePaymentDto(string OrderId, decimal Amount);

/// <summary>支付回调输入。</summary>
public record NotifyDto(Guid PaymentId, string NotifyId, decimal Amount, string Status, string Signature);

/// <summary>回调入账结果。</summary>
public record NotifyResult(NotifyKind Kind, PaymentRecord? Payment);

/// <summary>支付演示配置。</summary>
public record PaymentOptions(string NotifySecret);

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
