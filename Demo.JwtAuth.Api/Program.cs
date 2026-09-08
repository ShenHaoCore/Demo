using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

const string issuer = "Demo.JwtAuth.Api";
const string audience = "Demo.JwtAuth.Client";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("DemoJwtAuthSigningKey_AtLeast32Bytes!!"));

builder.Services.AddOpenApi();
builder.Services.AddSingleton<RefreshTokenStore>();
builder.Services.AddSingleton(new JwtTokenService(issuer, audience, signingKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "健康", service = "Demo.JwtAuth.Api" }));

app.MapPost("/api/auth/login", (LoginRequest request, JwtTokenService jwt, RefreshTokenStore refreshStore) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "用户名和密码不能为空" });

    // 演示用：任意非空用户名密码均可登录
    var accessToken = jwt.CreateAccessToken(request.Username.Trim());
    var refreshToken = refreshStore.Issue(request.Username.Trim());
    return Results.Ok(new
    {
        message = "登录成功",
        accessToken,
        refreshToken,
        expiresInSeconds = jwt.AccessTokenLifetimeSeconds
    });
});

app.MapPost("/api/auth/refresh", (RefreshRequest request, JwtTokenService jwt, RefreshTokenStore refreshStore) =>
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
        return Results.BadRequest(new { message = "refreshToken 不能为空" });

    if (!refreshStore.TryRotate(request.RefreshToken, out var username, out var newRefreshToken))
        return Results.Unauthorized();

    var accessToken = jwt.CreateAccessToken(username!);
    return Results.Ok(new
    {
        message = "刷新成功（旧 refresh 已失效）",
        accessToken,
        refreshToken = newRefreshToken,
        expiresInSeconds = jwt.AccessTokenLifetimeSeconds
    });
});

app.MapGet("/api/me", (ClaimsPrincipal user) =>
{
    var name = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? "未知";
    return Results.Ok(new { message = "当前用户", username = name });
}).RequireAuthorization();

app.Run();

record LoginRequest(string Username, string Password);
record RefreshRequest(string RefreshToken);

sealed class JwtTokenService(string issuer, string audience, SymmetricSecurityKey signingKey)
{
    public int AccessTokenLifetimeSeconds { get; } = 300;

    public string CreateAccessToken(string username)
    {
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: [new Claim(ClaimTypes.Name, username)],
            expires: DateTime.UtcNow.AddSeconds(AccessTokenLifetimeSeconds),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

sealed class RefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshEntry> _tokens = new();

    public string Issue(string username)
    {
        var token = CreateToken();
        _tokens[token] = new RefreshEntry(username, DateTime.UtcNow.AddDays(7));
        return token;
    }

    public bool TryRotate(string refreshToken, out string? username, out string? newRefreshToken)
    {
        username = null;
        newRefreshToken = null;

        if (!_tokens.TryRemove(refreshToken, out var entry))
            return false;

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
            return false;

        username = entry.Username;
        newRefreshToken = Issue(entry.Username);
        return true;
    }

    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private sealed record RefreshEntry(string Username, DateTime ExpiresAtUtc);
}
