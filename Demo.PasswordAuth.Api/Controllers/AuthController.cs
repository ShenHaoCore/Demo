using Demo.PasswordAuth.Api.Application;
using Demo.PasswordAuth.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.PasswordAuth.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserAppService _userAppService;

    public AuthController(IUserAppService userAppService) => _userAppService = userAppService;

    [HttpPost("register")]
    public IActionResult CreateAsync(CreateUserDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Password))
            return BadRequest(new { message = "用户名和密码不能为空" });

        if (!_userAppService.TryRegister(input))
            return Conflict(new { message = "用户名已存在" });

        return Ok(new { message = "注册成功", username = input.Username.Trim() });
    }

    [HttpPost("login")]
    public IActionResult LoginAsync(LoginDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Password))
            return BadRequest(new { message = "用户名和密码不能为空" });

        if (!_userAppService.TryLogin(input, out var token))
            return Unauthorized();

        return Ok(new { message = "登录成功", sessionToken = token });
    }

    [HttpGet("me")]
    public IActionResult GetAsync()
    {
        if (!TryGetBearerToken(Request, out var token) ||
            !_userAppService.TryGetSession(token!, out var username))
        {
            return Unauthorized();
        }

        return Ok(new { username, message = "会话有效" });
    }

    private static bool TryGetBearerToken(HttpRequest request, out string? token)
    {
        token = null;
        var header = request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) ||
            !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = header["Bearer ".Length..].Trim();
        return !string.IsNullOrEmpty(token);
    }
}
