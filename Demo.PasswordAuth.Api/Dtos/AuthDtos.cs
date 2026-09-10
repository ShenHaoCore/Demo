namespace Demo.PasswordAuth.Api.Dtos;

/// <summary>注册用户输入 DTO。</summary>
public sealed class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>登录输入 DTO。</summary>
public sealed class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
