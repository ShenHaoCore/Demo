using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<UserStore>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/health", () => Results.Ok(new { status = "健康", service = "Demo.PasswordAuth.Api" }));

app.MapPost("/api/register", (RegisterRequest request, UserStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "用户名和密码不能为空" });

    if (!store.TryRegister(request.Username.Trim(), request.Password))
        return Results.Conflict(new { message = "用户名已存在" });

    return Results.Ok(new { message = "注册成功", username = request.Username.Trim() });
});

app.MapPost("/api/login", (LoginRequest request, UserStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "用户名和密码不能为空" });

    if (!store.TryLogin(request.Username.Trim(), request.Password, out var token))
        return Results.Unauthorized();

    return Results.Ok(new { message = "登录成功", sessionToken = token });
});

app.Run();

record RegisterRequest(string Username, string Password);
record LoginRequest(string Username, string Password);

sealed record UserRecord(string Username, byte[] Salt, byte[] PasswordHash);

sealed class UserStore
{
    private readonly ConcurrentDictionary<string, UserRecord> _users =
        new(StringComparer.OrdinalIgnoreCase);

    public bool TryRegister(string username, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = HashPassword(password, salt);
        return _users.TryAdd(username, new UserRecord(username, salt, hash));
    }

    public bool TryLogin(string username, string password, out string? token)
    {
        token = null;
        if (!_users.TryGetValue(username, out var user))
            return false;

        var hash = HashPassword(password, user.Salt);
        if (!CryptographicOperations.FixedTimeEquals(hash, user.PasswordHash))
            return false;

        token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return true;
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 100_000,
            HashAlgorithmName.SHA256,
            outputLength: 32);
    }
}
