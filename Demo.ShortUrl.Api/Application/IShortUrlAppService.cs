using Demo.ShortUrl.Api.Dtos;

namespace Demo.ShortUrl.Api.Application;

/// <summary>短链应用服务契约。</summary>
public interface IShortUrlAppService
{
    /// <exception cref="ArgumentException">输入校验失败。</exception>
    ShortUrlCreatedDto Create(CreateShortUrlDto input);

    ShortUrlDto? Get(string code);

    /// <returns>跳转目标 URL；null 表示不存在。</returns>
    string? RedirectAndIncrement(string code);
}
