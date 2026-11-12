using Demo.ShortUrl.Api.Dtos;

namespace Demo.ShortUrl.Api.Application;

/// <summary>短链应用服务契约。</summary>
public interface IShortUrlAppService
{
    /// <exception cref="ArgumentException">输入校验失败。</exception>
    Task<ShortUrlCreatedDto> CreateAsync(CreateShortUrlDto input, CancellationToken cancellationToken = default);

    Task<ShortUrlDto?> GetAsync(string code, CancellationToken cancellationToken = default);

    /// <returns>跳转目标 URL；null 表示不存在。</returns>
    Task<string?> RedirectAndIncrementAsync(string code, CancellationToken cancellationToken = default);
}
