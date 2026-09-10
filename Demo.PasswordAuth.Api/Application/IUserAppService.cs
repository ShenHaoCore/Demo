using Demo.PasswordAuth.Api.Dtos;

namespace Demo.PasswordAuth.Api.Application;

/// <summary>用户应用服务契约。</summary>
public interface IUserAppService
{
    bool TryRegister(CreateUserDto input);

    bool TryLogin(LoginDto input, out string? token);

    bool TryGetSession(string token, out string? username);
}
