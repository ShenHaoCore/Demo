namespace Demo.PasswordAuth.Api.Entities;

/// <summary>用户实体（Domain）。</summary>
public sealed class UserRecord
{
    public string Username { get; init; } = string.Empty;
    public byte[] Salt { get; init; } = [];
    public byte[] PasswordHash { get; init; } = [];

    public UserRecord()
    {
    }

    public UserRecord(string username, byte[] salt, byte[] passwordHash)
    {
        Username = username;
        Salt = salt;
        PasswordHash = passwordHash;
    }
}
