namespace Demo.QrLogin.Api.Entities;

/// <summary>二维码登录状态。</summary>
public enum QrStatus
{
    Pending,
    Scanned,
    Confirmed,
    Expired
}

/// <summary>二维码登录票据实体。</summary>
public sealed class QrTicket
{
    public required string Id { get; init; }
    public QrStatus Status { get; set; } = QrStatus.Pending;
    public string? Username { get; set; }
    public string? Token { get; set; }
    public DateTime ExpiresAtUtc { get; init; }
}
