using Demo.DistributedLock.Api.Dtos;
using Demo.DistributedLock.Api.Entities;
using Demo.DistributedLock.Api.Services;

namespace Demo.DistributedLock.Api.Application;

/// <summary>锁管理应用服务。</summary>
public sealed class LockAppService(InMemoryLockService locks) : ILockAppService
{
    public Task<LockAcquireResult> AcquireAsync(string resource, AcquireLockDto? input)
    {
        var ttlSeconds = input?.TtlSeconds is > 0 ? input.TtlSeconds!.Value : 10;
        ttlSeconds = Math.Clamp(ttlSeconds, 1, 300);

        if (!locks.TryAcquire(resource, TimeSpan.FromSeconds(ttlSeconds), out var token, out var expiresAt))
        {
            return Task.FromResult(new LockAcquireResult
            {
                Success = false,
                Body = new
                {
                    message = "资源已被锁定（SET NX 未成功）",
                    resource,
                    locked = true
                }
            });
        }

        return Task.FromResult(new LockAcquireResult
        {
            Success = true,
            Body = new
            {
                message = "加锁成功（模拟 SET key token NX EX）",
                resource,
                token,
                ttlSeconds,
                expiresAt
            }
        });
    }

    public Task<LockReleaseResult> ReleaseAsync(string resource, ReleaseLockDto input)
    {
        var result = locks.TryRelease(resource, input.Token.Trim());
        return Task.FromResult(result switch
        {
            ReleaseStatus.Released => new LockReleaseResult
            {
                Status = result,
                Body = new { message = "解锁成功", resource }
            },
            ReleaseStatus.NotHeld => new LockReleaseResult
            {
                Status = result,
                Body = new { message = "锁不存在或已过期", resource }
            },
            ReleaseStatus.TokenMismatch => new LockReleaseResult
            {
                Status = result,
                Body = new
                {
                    message = "token 不匹配，拒绝解锁（防止误删他人锁）",
                    resource
                }
            },
            _ => new LockReleaseResult
            {
                Status = result,
                Body = new { message = "未知状态", resource }
            }
        });
    }

    public Task<object> GetInfoAsync(string resource)
    {
        var info = locks.GetInfo(resource);
        if (info is null)
        {
            return Task.FromResult<object>(new { resource, locked = false });
        }

        return Task.FromResult<object>(new
        {
            resource,
            locked = true,
            expiresAt = info.Value.ExpiresAt,
            // 不返回完整 token，避免误用；仅演示
            tokenPrefix = info.Value.Token[..Math.Min(8, info.Value.Token.Length)]
        });
    }
}
