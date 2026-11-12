using System.Collections.Concurrent;
using Demo.ShortUrl.Api.Entities;

namespace Demo.ShortUrl.Api.Services;

/// <summary>
/// 进程内实现，命令语义与 <see cref="RedisShortUrlStore"/> 对齐，便于无 Redis 时本地跑。
/// shortcut: 非跨进程，上 Redis 时改 Provider=Redis。
/// </summary>
public sealed class MemoryShortUrlStore : IShortUrlStore
{
    private long _seq;
    private readonly ConcurrentDictionary<string, ShortUrlEntry> _urls = new(StringComparer.Ordinal);

    public string ProviderName => "Memory";

    public Task<long> NextIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Interlocked.Increment(ref _seq));
    }

    public Task SaveAsync(ShortUrlEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entry);
        if (!_urls.TryAdd(entry.Code, entry))
        {
            throw new InvalidOperationException($"短码已存在: {entry.Code}");
        }

        return Task.CompletedTask;
    }

    public Task<ShortUrlEntry?> FindAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _urls.TryGetValue(code, out var entry);
        return Task.FromResult(entry);
    }

    public Task<(string LongUrl, long VisitCount)?> RedirectAndIncrementAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_urls.TryGetValue(code, out var entry))
        {
            return Task.FromResult<(string, long)?>(null);
        }

        var visits = entry.IncrementVisit();
        return Task.FromResult<(string, long)?>((entry.LongUrl, visits));
    }
}
