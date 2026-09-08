using System.Collections.Concurrent;
using System.Security.Cryptography;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<QrLoginStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "健康", service = "Demo.QrLogin.Api" }));

app.MapPost("/api/qr/create", (QrLoginStore store) =>
{
    var ticket = store.Create();
    return Results.Ok(new
    {
        message = "二维码票据已创建",
        ticket = ticket.Id,
        status = ticket.Status.ToString(),
        expiresAtUtc = ticket.ExpiresAtUtc
    });
});

app.MapPost("/api/qr/{ticket}/scan", (string ticket, ScanRequest? request, QrLoginStore store) =>
{
    var username = string.IsNullOrWhiteSpace(request?.Username) ? "演示用户" : request!.Username.Trim();
    if (!store.TryScan(ticket, username, out var error))
        return Results.BadRequest(new { message = error });

    return Results.Ok(new { message = "扫码成功，等待确认", ticket, status = QrStatus.Scanned.ToString(), username });
});

app.MapPost("/api/qr/{ticket}/confirm", (string ticket, QrLoginStore store) =>
{
    if (!store.TryConfirm(ticket, out var token, out var error))
        return Results.BadRequest(new { message = error });

    return Results.Ok(new
    {
        message = "确认成功，PC 端可领取登录态",
        ticket,
        status = QrStatus.Confirmed.ToString(),
        token
    });
});

app.MapGet("/api/qr/{ticket}/status", (string ticket, QrLoginStore store) =>
{
    if (!store.TryGetStatus(ticket, out var info, out var error))
        return Results.NotFound(new { message = error });

    return Results.Ok(info);
});

app.Run();

record ScanRequest(string? Username);

enum QrStatus
{
    Pending,
    Scanned,
    Confirmed,
    Expired
}

sealed class QrTicket
{
    public required string Id { get; init; }
    public QrStatus Status { get; set; } = QrStatus.Pending;
    public string? Username { get; set; }
    public string? Token { get; set; }
    public DateTime ExpiresAtUtc { get; init; }
}

sealed class QrLoginStore
{
    private readonly ConcurrentDictionary<string, QrTicket> _tickets = new();
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    public QrTicket Create()
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var ticket = new QrTicket
        {
            Id = id,
            ExpiresAtUtc = DateTime.UtcNow.Add(Lifetime)
        };
        _tickets[id] = ticket;
        return ticket;
    }

    public bool TryScan(string ticketId, string username, out string? error)
    {
        error = null;
        if (!TryGetFresh(ticketId, out var ticket, out error))
            return false;

        if (ticket!.Status is not QrStatus.Pending)
        {
            error = $"当前状态为 {ticket.Status}，无法扫码";
            return false;
        }

        ticket.Username = username;
        ticket.Status = QrStatus.Scanned;
        return true;
    }

    public bool TryConfirm(string ticketId, out string? token, out string? error)
    {
        token = null;
        error = null;
        if (!TryGetFresh(ticketId, out var ticket, out error))
            return false;

        if (ticket!.Status is not QrStatus.Scanned)
        {
            error = $"当前状态为 {ticket.Status}，无法确认（需先扫码）";
            return false;
        }

        token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        ticket.Token = token;
        ticket.Status = QrStatus.Confirmed;
        return true;
    }

    public bool TryGetStatus(string ticketId, out object? info, out string? error)
    {
        info = null;
        error = null;
        if (!_tickets.TryGetValue(ticketId, out var ticket))
        {
            error = "票据不存在";
            return false;
        }

        MarkExpiredIfNeeded(ticket);

        info = new
        {
            ticket = ticket.Id,
            status = ticket.Status.ToString(),
            username = ticket.Username,
            token = ticket.Status == QrStatus.Confirmed ? ticket.Token : null,
            expiresAtUtc = ticket.ExpiresAtUtc,
            message = ticket.Status switch
            {
                QrStatus.Pending => "等待扫码",
                QrStatus.Scanned => "已扫码，等待确认",
                QrStatus.Confirmed => "已确认，可使用 token 登录",
                QrStatus.Expired => "已过期",
                _ => "未知状态"
            }
        };
        return true;
    }

    private bool TryGetFresh(string ticketId, out QrTicket? ticket, out string? error)
    {
        error = null;
        if (!_tickets.TryGetValue(ticketId, out ticket))
        {
            error = "票据不存在";
            return false;
        }

        MarkExpiredIfNeeded(ticket);
        if (ticket.Status == QrStatus.Expired)
        {
            error = "票据已过期";
            return false;
        }

        return true;
    }

    private static void MarkExpiredIfNeeded(QrTicket ticket)
    {
        if (ticket.Status != QrStatus.Expired && ticket.ExpiresAtUtc < DateTime.UtcNow)
            ticket.Status = QrStatus.Expired;
    }
}
