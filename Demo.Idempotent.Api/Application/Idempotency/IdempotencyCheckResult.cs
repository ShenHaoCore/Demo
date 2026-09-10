namespace Demo.Idempotent.Api.Application.Idempotency;

/// <summary>幂等检查返回值。</summary>
public readonly record struct IdempotencyCheckResult(
    IdempotencyCheckStatus Status,
    IdempotencySnapshot? Snapshot);
