namespace Demo.Idempotent.Api.Entities;

/// <summary>幂等记录（DB 权威；Key 唯一）。</summary>
public sealed class IdempotencyEntry
{
    public string Key { get; set; } = string.Empty;
    public string BodyHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public int StatusCode { get; set; }
    public string ResponseBodyJson { get; set; } = string.Empty;
    public string? Location { get; set; }
    public Guid OrderId { get; set; }
}

