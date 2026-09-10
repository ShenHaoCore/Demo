namespace Demo.JwtAuth.Api.Dtos;

/// <summary>登录输入。</summary>
public sealed class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>刷新令牌输入。</summary>
public sealed class RefreshDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>令牌输出。</summary>
public sealed class TokenDto
{
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

/// <summary>当前用户输出。</summary>
public sealed class CurrentUserDto
{
    public string Message { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
