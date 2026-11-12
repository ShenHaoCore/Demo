using Demo.ShortUrl.Api.Entities;

namespace Demo.ShortUrl.Api.Services;

/// <summary>短链存储：发号、映射、原子计次（Redis 命令语义）。</summary>
public interface IShortUrlStore
{
    string ProviderName { get; }

    /// <summary>发号机：等价 Redis INCR。</summary>
    Task<long> NextIdAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ShortUrlEntry entry, CancellationToken cancellationToken = default);

    Task<ShortUrlEntry?> FindAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>跳转计次：等价 Redis INCR；映射不存在返回 null。</summary>
    Task<(string LongUrl, long VisitCount)?> RedirectAndIncrementAsync(
        string code,
        CancellationToken cancellationToken = default);
}
