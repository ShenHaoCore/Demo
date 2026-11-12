using Demo.ShortUrl.Api.Entities;
using StackExchange.Redis;

namespace Demo.ShortUrl.Api.Services;

/// <summary>生产路径：Redis INCR 发号 + Hash 映射 + INCR 计次。</summary>
public sealed class RedisShortUrlStore(IConnectionMultiplexer mux) : IShortUrlStore
{
    private const string SeqKey = "su:seq";
    private static readonly RedisValue UrlField = "url";
    private static readonly RedisValue AtField = "at";

    private readonly IDatabase _db = mux.GetDatabase();

    public string ProviderName => "Redis";

    public async Task<long> NextIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _db.StringIncrementAsync(SeqKey).ConfigureAwait(false);
    }

    public async Task SaveAsync(ShortUrlEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        var key = MappingKey(entry.Code);
        // 发号机保证短码唯一；EXISTS 仅作防重放/误调用
        if (await _db.KeyExistsAsync(key).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"短码已存在: {entry.Code}");
        }

        await _db.HashSetAsync(
                key,
                [
                    new HashEntry(UrlField, entry.LongUrl),
                    new HashEntry(AtField, entry.CreatedAt.ToUnixTimeMilliseconds())
                ])
            .ConfigureAwait(false);

        // 计次键独立，跳转只 INCR，不改 Hash
        await _db.StringSetAsync(ClicksKey(entry.Code), 0, when: When.NotExists).ConfigureAwait(false);
    }

    public async Task<ShortUrlEntry?> FindAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hash = await _db.HashGetAllAsync(MappingKey(code)).ConfigureAwait(false);
        if (hash.Length == 0)
        {
            return null;
        }

        string? url = null;
        long atMs = 0;
        foreach (var field in hash)
        {
            if (field.Name == UrlField)
            {
                url = field.Value;
            }
            else if (field.Name == AtField && field.Value.TryParse(out long ms))
            {
                atMs = ms;
            }
        }

        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        var clicksRaw = await _db.StringGetAsync(ClicksKey(code)).ConfigureAwait(false);
        var clicks = clicksRaw.IsNullOrEmpty ? 0L : (long)clicksRaw;
        return new ShortUrlEntry(
            code,
            url,
            DateTimeOffset.FromUnixTimeMilliseconds(atMs),
            clicks);
    }

    public async Task<(string LongUrl, long VisitCount)?> RedirectAndIncrementAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var url = await _db.HashGetAsync(MappingKey(code), UrlField).ConfigureAwait(false);
        if (url.IsNullOrEmpty)
        {
            return null;
        }

        var visits = await _db.StringIncrementAsync(ClicksKey(code)).ConfigureAwait(false);
        return (url.ToString(), visits);
    }

    private static string MappingKey(string code) => $"su:u:{code}";

    private static string ClicksKey(string code) => $"su:c:{code}";
}
