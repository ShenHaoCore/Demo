using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Demo.PasswordAuth.Api.Dtos;
using Demo.PasswordAuth.Api.Entities;

namespace Demo.PasswordAuth.Api.Application;

/// <summary>用户应用服务。</summary>
public sealed class UserAppService : IUserAppService
{
    private readonly ConcurrentDictionary<string, UserRecord> _users =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _sessions =
        new(StringComparer.Ordinal); // token -> username

    public bool TryRegister(CreateUserDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var username = input.Username.Trim();
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = HashPassword(input.Password, salt);
        return _users.TryAdd(username, new UserRecord(username, salt, hash));
    }

    public bool TryLogin(LoginDto input, out string? token)
    {
        ArgumentNullException.ThrowIfNull(input);
        token = null;
        var username = input.Username.Trim();
        if (!_users.TryGetValue(username, out var user))
            return false;

        var hash = HashPassword(input.Password, user.Salt);
        if (!CryptographicOperations.FixedTimeEquals(hash, user.PasswordHash))
            return false;

        token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = username;
        return true;
    }

    public bool TryGetSession(string token, out string? username) =>
        _sessions.TryGetValue(token, out username);

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
