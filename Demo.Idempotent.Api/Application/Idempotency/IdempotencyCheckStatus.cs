namespace Demo.Idempotent.Api.Application.Idempotency;

/// <summary>幂等检查结果状态。</summary>
public enum IdempotencyCheckStatus
{
    Miss,
    Replay,
    ConflictBody
}
