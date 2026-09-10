using Demo.DistributedLock.Api.Dtos;
using Demo.DistributedLock.Api.Services;

namespace Demo.DistributedLock.Api.Application;

/// <summary>临界区演示应用服务。</summary>
public sealed class CriticalAppService(InMemoryLockService locks) : ICriticalAppService
{
    public async Task<CriticalExecuteResult> ExecuteAsync(string resource, CriticalDto? input)
    {
        var workMs = Math.Clamp(input?.WorkMs ?? 500, 10, 5000);
        var ttlSeconds = Math.Clamp(input?.TtlSeconds ?? 5, 1, 60);

        if (!locks.TryAcquire(resource, TimeSpan.FromSeconds(ttlSeconds), out var token, out _))
        {
            return new CriticalExecuteResult
            {
                IsConflict = true,
                Body = new
                {
                    message = "并发争用：未能获取锁，关键区执行被拒绝",
                    resource,
                    status = 409
                }
            };
        }

        try
        {
            await Task.Delay(workMs);
            return new CriticalExecuteResult
            {
                Body = new
                {
                    message = "持锁执行完成",
                    resource,
                    token,
                    workMs
                }
            };
        }
        finally
        {
            locks.TryRelease(resource, token);
        }
    }
}
