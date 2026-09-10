namespace Demo.Idempotent.Api.Options;

/// <summary>幂等选项（Demo 默认 24h，对齐常见支付网关 TTL）。</summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>幂等记录存活时间；过期后同 Key 可再次用于「新订单」，不删除历史订单。</summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(24);
}

