using System.Collections.Concurrent;
using System.Security.Cryptography;
using Demo.QrLogin.Api.Dtos;
using Demo.QrLogin.Api.Entities;

namespace Demo.QrLogin.Api.Application;

/// <summary>扫码登录应用服务。</summary>
public sealed class QrLoginAppService : IQrLoginAppService
{
    private readonly ConcurrentDictionary<string, QrTicket> _tickets = new();
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    public QrTicketCreatedDto Create()
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var ticket = new QrTicket
        {
            Id = id,
            ExpiresAtUtc = DateTime.UtcNow.Add(Lifetime)
        };
        _tickets[id] = ticket;
        return new QrTicketCreatedDto
        {
            Message = "二维码票据已创建",
            Ticket = ticket.Id,
            Status = ticket.Status.ToString(),
            ExpiresAtUtc = ticket.ExpiresAtUtc
        };
    }

    public QrScanResultDto Scan(string ticketId, ScanDto? input)
    {
        var username = string.IsNullOrWhiteSpace(input?.Username) ? "演示用户" : input!.Username.Trim();
        if (!TryGetFresh(ticketId, out var ticket, out var error))
        {
            throw new InvalidOperationException(error);
        }

        if (ticket!.Status is not QrStatus.Pending)
        {
            throw new InvalidOperationException($"当前状态为 {ticket.Status}，无法扫码");
        }

        ticket.Username = username;
        ticket.Status = QrStatus.Scanned;
        return new QrScanResultDto
        {
            Message = "扫码成功，等待确认",
            Ticket = ticketId,
            Status = QrStatus.Scanned.ToString(),
            Username = username
        };
    }

    public QrConfirmResultDto Confirm(string ticketId)
    {
        if (!TryGetFresh(ticketId, out var ticket, out var error))
        {
            throw new InvalidOperationException(error);
        }

        if (ticket!.Status is not QrStatus.Scanned)
        {
            throw new InvalidOperationException($"当前状态为 {ticket.Status}，无法确认（需先扫码）");
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        ticket.Token = token;
        ticket.Status = QrStatus.Confirmed;
        return new QrConfirmResultDto
        {
            Message = "确认成功，PC 端可领取登录态",
            Ticket = ticketId,
            Status = QrStatus.Confirmed.ToString(),
            Token = token
        };
    }

    public QrStatusDto? GetStatus(string ticketId)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket))
        {
            return null;
        }

        MarkExpiredIfNeeded(ticket);

        return new QrStatusDto
        {
            Ticket = ticket.Id,
            Status = ticket.Status.ToString(),
            Username = ticket.Username,
            Token = ticket.Status == QrStatus.Confirmed ? ticket.Token : null,
            ExpiresAtUtc = ticket.ExpiresAtUtc,
            Message = ticket.Status switch
            {
                QrStatus.Pending => "等待扫码",
                QrStatus.Scanned => "已扫码，等待确认",
                QrStatus.Confirmed => "已确认，可使用 token 登录",
                QrStatus.Expired => "已过期",
                _ => "未知状态"
            }
        };
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
        {
            ticket.Status = QrStatus.Expired;
        }
    }
}
