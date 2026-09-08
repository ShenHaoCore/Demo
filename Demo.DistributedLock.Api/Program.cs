using System.Collections.Concurrent;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<InMemoryLockService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.DistributedLock.Api" }));

app.MapPost("/api/locks/{resource}/acquire", (string resource, AcquireLockRequest? request, InMemoryLockService locks) =>
{
    if (string.IsNullOrWhiteSpace(resource))
    {
        return Results.BadRequest(new { message = "resource 不能为空" });
    }

    var ttlSeconds = request?.TtlSeconds is > 0 ? request.TtlSeconds!.Value : 10;
    ttlSeconds = Math.Clamp(ttlSeconds, 1, 300);

    if (!locks.TryAcquire(resource, TimeSpan.FromSeconds(ttlSeconds), out var token, out var expiresAt))
    {
        return Results.Conflict(new
        {
            message = "资源已被锁定（SET NX 未成功）",
            resource,
            locked = true
        });
    }

    return Results.Ok(new
    {
        message = "加锁成功（模拟 SET key token NX EX）",
        resource,
        token,
        ttlSeconds,
        expiresAt
    });
})
.WithName("AcquireLock")
.WithSummary("获取分布式锁（SET NX EX）");

app.MapPost("/api/locks/{resource}/release", (string resource, ReleaseLockRequest request, InMemoryLockService locks) =>
{
    if (string.IsNullOrWhiteSpace(resource))
    {
        return Results.BadRequest(new { message = "resource 不能为空" });
    }

    if (request is null || string.IsNullOrWhiteSpace(request.Token))
    {
        return Results.BadRequest(new { message = "必须提供 token" });
    }

    var result = locks.TryRelease(resource, request.Token.Trim());
    return result switch
    {
        ReleaseStatus.Released => Results.Ok(new { message = "解锁成功", resource }),
        ReleaseStatus.NotHeld => Results.NotFound(new { message = "锁不存在或已过期", resource }),
        ReleaseStatus.TokenMismatch => Results.Conflict(new
        {
            message = "token 不匹配，拒绝解锁（防止误删他人锁）",
            resource
        }),
        _ => Results.StatusCode(500)
    };
})
.WithName("ReleaseLock")
.WithSummary("带 token 释放锁；错误 token 拒绝");

app.MapPost("/api/critical/{resource}", async (string resource, CriticalRequest? request, InMemoryLockService locks) =>
{
    if (string.IsNullOrWhiteSpace(resource))
    {
        return Results.BadRequest(new { message = "resource 不能为空" });
    }

    var workMs = Math.Clamp(request?.WorkMs ?? 500, 10, 5000);
    var ttlSeconds = Math.Clamp(request?.TtlSeconds ?? 5, 1, 60);

    if (!locks.TryAcquire(resource, TimeSpan.FromSeconds(ttlSeconds), out var token, out _))
    {
        return Results.Conflict(new
        {
            message = "并发争用：未能获取锁，关键区执行被拒绝",
            resource,
            status = 409
        });
    }

    try
    {
        await Task.Delay(workMs);
        return Results.Ok(new
        {
            message = "持锁执行完成",
            resource,
            token,
            workMs
        });
    }
    finally
    {
        locks.TryRelease(resource, token);
    }
})
.WithName("CriticalSection")
.WithSummary("演示持锁执行临界区；并发争用返回 409");

app.MapGet("/api/locks/{resource}", (string resource, InMemoryLockService locks) =>
{
    var info = locks.GetInfo(resource);
    if (info is null)
    {
        return Results.Ok(new { resource, locked = false });
    }

    return Results.Ok(new
    {
        resource,
        locked = true,
        expiresAt = info.Value.ExpiresAt,
        // 不返回完整 token，避免误用；仅演示
        tokenPrefix = info.Value.Token[..Math.Min(8, info.Value.Token.Length)]
    });
})
.WithName("GetLockInfo")
.WithSummary("查看资源锁状态");

app.Run();

sealed class InMemoryLockService
{
    private readonly ConcurrentDictionary<string, LockEntry> _locks = new(StringComparer.Ordinal);

    public bool TryAcquire(string resource, TimeSpan ttl, out string token, out DateTimeOffset expiresAt)
    {
        CleanupIfExpired(resource);

        token = Guid.NewGuid().ToString("N");
        expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        var entry = new LockEntry(token, expiresAt);

        if (_locks.TryAdd(resource, entry))
        {
            return true;
        }

        // 可能刚好过期，再试一次
        CleanupIfExpired(resource);
        if (_locks.TryAdd(resource, entry))
        {
            return true;
        }

        token = string.Empty;
        expiresAt = default;
        return false;
    }

    public ReleaseStatus TryRelease(string resource, string token)
    {
        CleanupIfExpired(resource);

        if (!_locks.TryGetValue(resource, out var entry))
        {
            return ReleaseStatus.NotHeld;
        }

        if (!string.Equals(entry.Token, token, StringComparison.Ordinal))
        {
            return ReleaseStatus.TokenMismatch;
        }

        // 仅当仍是同一 entry 时删除
        if (_locks.TryRemove(new KeyValuePair<string, LockEntry>(resource, entry)))
        {
            return ReleaseStatus.Released;
        }

        return ReleaseStatus.NotHeld;
    }

    public (string Token, DateTimeOffset ExpiresAt)? GetInfo(string resource)
    {
        CleanupIfExpired(resource);
        return _locks.TryGetValue(resource, out var entry)
            ? (entry.Token, entry.ExpiresAt)
            : null;
    }

    private void CleanupIfExpired(string resource)
    {
        if (_locks.TryGetValue(resource, out var entry) && entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _locks.TryRemove(new KeyValuePair<string, LockEntry>(resource, entry));
        }
    }
}

enum ReleaseStatus
{
    Released,
    NotHeld,
    TokenMismatch
}

record LockEntry(string Token, DateTimeOffset ExpiresAt);

record AcquireLockRequest(int? TtlSeconds);

record ReleaseLockRequest(string Token);

record CriticalRequest(int? WorkMs, int? TtlSeconds);
