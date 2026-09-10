using System.Collections.Concurrent;
using Demo.DistributedLock.Api.Entities;

namespace Demo.DistributedLock.Api.Services;

/// <summary>内存版分布式锁（模拟 SET NX EX）。</summary>
public sealed class InMemoryLockService
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
