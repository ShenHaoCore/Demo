using Demo.JwtAuth.Api.Dtos;

namespace Demo.JwtAuth.Api.Application;

/// <summary>认证应用服务契约。</summary>
public interface IAuthAppService
{
    /// <exception cref="ArgumentException">输入校验失败。</exception>
    TokenDto Login(LoginDto input);

    /// <exception cref="UnauthorizedAccessException">refresh 无效或已过期。</exception>
    /// <exception cref="ArgumentException">输入校验失败。</exception>
    TokenDto Refresh(RefreshDto input);
}
