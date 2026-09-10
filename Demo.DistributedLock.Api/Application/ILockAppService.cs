using Demo.DistributedLock.Api.Dtos;
using Demo.DistributedLock.Api.Entities;

namespace Demo.DistributedLock.Api.Application;

/// <summary>锁管理应用服务契约。</summary>
public interface ILockAppService
{
    Task<LockAcquireResult> AcquireAsync(string resource, AcquireLockDto? input);

    Task<LockReleaseResult> ReleaseAsync(string resource, ReleaseLockDto input);

    Task<object> GetInfoAsync(string resource);
}

/// <summary>加锁结果。</summary>
public sealed class LockAcquireResult
{
    public bool Success { get; init; }
    public object Body { get; init; } = default!;
}

/// <summary>解锁结果。</summary>
public sealed class LockReleaseResult
{
    public ReleaseStatus Status { get; init; }
    public object Body { get; init; } = default!;
}
