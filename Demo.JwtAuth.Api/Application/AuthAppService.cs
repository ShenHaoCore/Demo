using System.Collections.Concurrent;
using System.Security.Cryptography;
using Demo.JwtAuth.Api.Dtos;
using Demo.JwtAuth.Api.Services;

namespace Demo.JwtAuth.Api.Application;

/// <summary>认证应用服务（含 refresh token 轮换）。</summary>
public sealed class AuthAppService(JwtTokenService jwt) : IAuthAppService
{
    private readonly ConcurrentDictionary<string, RefreshEntry> _tokens = new();

    public TokenDto Login(LoginDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Password))
        {
            throw new ArgumentException("用户名和密码不能为空");
        }

        var username = input.Username.Trim();
        // 演示用：任意非空用户名密码均可登录
        return new TokenDto
        {
            Message = "登录成功",
            AccessToken = jwt.CreateAccessToken(username),
            RefreshToken = Issue(username),
            ExpiresInSeconds = jwt.AccessTokenLifetimeSeconds
        };
    }

    public TokenDto Refresh(RefreshDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.RefreshToken))
        {
            throw new ArgumentException("refreshToken 不能为空");
        }

        if (!TryRotate(input.RefreshToken, out var username, out var newRefreshToken))
        {
            throw new UnauthorizedAccessException();
        }

        return new TokenDto
        {
            Message = "刷新成功（旧 refresh 已失效）",
            AccessToken = jwt.CreateAccessToken(username!),
            RefreshToken = newRefreshToken!,
            ExpiresInSeconds = jwt.AccessTokenLifetimeSeconds
        };
    }

    private string Issue(string username)
    {
        var token = CreateToken();
        _tokens[token] = new RefreshEntry(username, DateTime.UtcNow.AddDays(7));
        return token;
    }

    private bool TryRotate(string refreshToken, out string? username, out string? newRefreshToken)
    {
        username = null;
        newRefreshToken = null;

        if (!_tokens.TryRemove(refreshToken, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
        {
            return false;
        }

        username = entry.Username;
        newRefreshToken = Issue(entry.Username);
        return true;
    }

    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private sealed record RefreshEntry(string Username, DateTime ExpiresAtUtc);
}
