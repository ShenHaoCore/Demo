using Demo.ShortUrl.Api.Dtos;
using Demo.ShortUrl.Api.Entities;
using Demo.ShortUrl.Api.Options;
using Demo.ShortUrl.Api.Services;
using Microsoft.Extensions.Options;

namespace Demo.ShortUrl.Api.Application;

/// <summary>短链应用服务：发号机 + Base62，计次走 Store INCR。</summary>
public sealed class ShortUrlAppService(
    IShortUrlStore store,
    IOptions<ShortUrlOptions> options) : IShortUrlAppService
{
    private readonly int _codeLength = Math.Clamp(options.Value.CodeLength, 4, 12);

    public async Task<ShortUrlCreatedDto> CreateAsync(
        CreateShortUrlDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.LongUrl))
        {
            throw new ArgumentException("请提供有效的 longUrl");
        }

        if (!Uri.TryCreate(input.LongUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("longUrl 必须是有效的 http/https 绝对地址");
        }

        var id = await store.NextIdAsync(cancellationToken).ConfigureAwait(false);
        var code = Base62.Encode(id, _codeLength);
        var entity = new ShortUrlEntry(code, uri.ToString(), DateTimeOffset.UtcNow, 0);
        await store.SaveAsync(entity, cancellationToken).ConfigureAwait(false);

        return new ShortUrlCreatedDto
        {
            Code = entity.Code,
            ShortPath = $"/{entity.Code}",
            LongUrl = entity.LongUrl,
            CreatedAt = entity.CreatedAt,
            VisitCount = entity.VisitCount,
            Message = $"短链已创建（{store.ProviderName} / 发号机+Base62）"
        };
    }

    public async Task<ShortUrlDto?> GetAsync(string code, CancellationToken cancellationToken = default)
    {
        var entity = await store.FindAsync(code, cancellationToken).ConfigureAwait(false);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<string?> RedirectAndIncrementAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var result = await store.RedirectAndIncrementAsync(code, cancellationToken).ConfigureAwait(false);
        return result?.LongUrl;
    }

    private static ShortUrlDto MapToDto(ShortUrlEntry entity) => new()
    {
        Code = entity.Code,
        LongUrl = entity.LongUrl,
        CreatedAt = entity.CreatedAt,
        VisitCount = entity.VisitCount
    };
}
