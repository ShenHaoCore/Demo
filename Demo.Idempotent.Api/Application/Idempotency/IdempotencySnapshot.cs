namespace Demo.Idempotent.Api.Application.Idempotency;

/// <summary>幂等响应快照（用于回放，非实体）。</summary>
public sealed record IdempotencySnapshot(
    string BodyHash,
    int StatusCode,
    string ResponseBodyJson,
    string? Location);
