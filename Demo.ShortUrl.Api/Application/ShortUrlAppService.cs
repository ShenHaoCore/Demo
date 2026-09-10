using System.Collections.Concurrent;
using System.Security.Cryptography;
using Demo.ShortUrl.Api.Dtos;
using Demo.ShortUrl.Api.Entities;

namespace Demo.ShortUrl.Api.Application;

/// <summary>短链应用服务。</summary>
public sealed class ShortUrlAppService : IShortUrlAppService
{
    private readonly ConcurrentDictionary<string, ShortUrlEntry> _urls = new(StringComparer.Ordinal);
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public ShortUrlCreatedDto Create(CreateShortUrlDto input)
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

        string code;
        ShortUrlEntry entity;
        do
        {
            code = GenerateCode(7);
            entity = new ShortUrlEntry(code, uri.ToString(), DateTimeOffset.UtcNow, 0);
        } while (!_urls.TryAdd(code, entity));

        return new ShortUrlCreatedDto
        {
            Code = entity.Code,
            ShortPath = $"/{entity.Code}",
            LongUrl = entity.LongUrl,
            CreatedAt = entity.CreatedAt,
            VisitCount = entity.VisitCount,
            Message = "短链已创建"
        };
    }

    public ShortUrlDto? Get(string code)
    {
        if (!_urls.TryGetValue(code, out var entity))
        {
            return null;
        }

        return MapToDto(entity);
    }

    public string? RedirectAndIncrement(string code)
    {
        if (!_urls.TryGetValue(code, out var entity))
        {
            return null;
        }

        entity.VisitCount++;
        return entity.LongUrl;
    }

    private static ShortUrlDto MapToDto(ShortUrlEntry entity) => new()
    {
        Code = entity.Code,
        LongUrl = entity.LongUrl,
        CreatedAt = entity.CreatedAt,
        VisitCount = entity.VisitCount
    };

    private static string GenerateCode(int length)
    {
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }
}
