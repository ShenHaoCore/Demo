namespace Demo.DistributedLock.Api.Entities;

/// <summary>内存锁条目。</summary>
public record LockEntry(string Token, DateTimeOffset ExpiresAt);

/// <summary>释放锁结果。</summary>
public enum ReleaseStatus
{
    Released,
    NotHeld,
    TokenMismatch
}
