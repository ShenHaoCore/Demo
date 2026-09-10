namespace Demo.Sso.Api.Dtos;

/// <summary>SSO 登录输入。</summary>
public sealed class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>校验 ticket 输入。</summary>
public sealed class ValidateDto
{
    public string Ticket { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
}

/// <summary>客户端输出。</summary>
public sealed class SsoClientDto
{
    public string Name { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
}

/// <summary>客户端列表输出。</summary>
public sealed class SsoClientListDto
{
    public string Message { get; set; } = string.Empty;
    public List<SsoClientDto> Clients { get; set; } = [];
}

/// <summary>登录输出。</summary>
public sealed class SsoLoginResultDto
{
    public string Message { get; set; } = string.Empty;
    public string SsoSessionId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
}

/// <summary>签发 ticket 输出。</summary>
public sealed class SsoTicketResultDto
{
    public string Message { get; set; } = string.Empty;
    public string Ticket { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
}

/// <summary>校验输出。</summary>
public sealed class SsoValidateResultDto
{
    public string Message { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
}
