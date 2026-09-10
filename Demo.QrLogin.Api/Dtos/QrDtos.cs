namespace Demo.QrLogin.Api.Dtos;

/// <summary>扫码输入。</summary>
public sealed class ScanDto
{
    public string? Username { get; set; }
}

/// <summary>创建票据输出。</summary>
public sealed class QrTicketCreatedDto
{
    public string Message { get; set; } = string.Empty;
    public string Ticket { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>扫码输出。</summary>
public sealed class QrScanResultDto
{
    public string Message { get; set; } = string.Empty;
    public string Ticket { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

/// <summary>确认输出。</summary>
public sealed class QrConfirmResultDto
{
    public string Message { get; set; } = string.Empty;
    public string Ticket { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Token { get; set; }
}

/// <summary>状态查询输出。</summary>
public sealed class QrStatusDto
{
    public string Ticket { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}
