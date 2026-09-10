using System.Collections.Concurrent;
using System.Security.Cryptography;
using Demo.Sso.Api.Dtos;
using Demo.Sso.Api.Entities;

namespace Demo.Sso.Api.Application;

/// <summary>SSO 应用服务（CAS 风格演示）。</summary>
public sealed class SsoAppService : ISsoAppService
{
    private readonly IReadOnlyList<SsoClient> _clients =
    [
        new("假客户端 A - 门户", "https://app-a.demo.local/callback"),
        new("假客户端 B - 后台", "https://app-b.demo.local/callback")
    ];

    private readonly ConcurrentDictionary<string, string> _sessions = new();
    private readonly ConcurrentDictionary<string, TicketEntry> _tickets = new();

    public SsoClientListDto GetClients() => new()
    {
        Message = "演示用假客户端（service）",
        Clients = _clients.Select(c => new SsoClientDto { Name = c.Name, Service = c.ServiceUrl }).ToList()
    };

    public SsoLoginResultDto Login(LoginDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Password))
        {
            throw new ArgumentException("用户名和密码不能为空");
        }

        var username = input.Username.Trim();
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _sessions[sessionId] = username;

        return new SsoLoginResultDto
        {
            Message = "SSO 登录成功（演示：任意非空密码均可）",
            SsoSessionId = sessionId,
            Username = username,
            Hint = "随后用 session 向 /api/sso/ticket?service= 换取一次性 ticket"
        };
    }

    public SsoTicketResultDto IssueTicket(string ssoSessionId, string service)
    {
        if (string.IsNullOrWhiteSpace(service))
        {
            throw new ArgumentException("必须提供 service 参数");
        }

        if (string.IsNullOrWhiteSpace(ssoSessionId))
        {
            throw new ArgumentException("必须提供 ssoSessionId（演示简化：用 query 传递）");
        }

        if (!_sessions.TryGetValue(ssoSessionId, out _))
        {
            throw new InvalidOperationException("SSO 会话无效，请先登录");
        }

        if (_clients.All(c => !string.Equals(c.ServiceUrl, service, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"未知的 service，仅支持演示客户端：{string.Join("、", _clients.Select(c => c.ServiceUrl))}");
        }

        var ticket = "ST-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        var username = _sessions[ssoSessionId];
        _tickets[ticket] = new TicketEntry(username, service, DateTime.UtcNow.AddMinutes(5), Consumed: false);

        return new SsoTicketResultDto
        {
            Message = "已签发一次性 CAS 风格 ticket",
            Ticket = ticket,
            Service = service,
            Hint = "客户端应携带 ticket 调用 /api/sso/validate 完成校验，ticket 仅能使用一次"
        };
    }

    public SsoValidateResultDto Validate(ValidateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Ticket) || string.IsNullOrWhiteSpace(input.Service))
        {
            throw new ArgumentException("ticket 与 service 均不能为空");
        }

        while (true)
        {
            if (!_tickets.TryGetValue(input.Ticket, out var entry))
            {
                throw new InvalidOperationException("ticket 无效");
            }

            if (entry.Consumed)
            {
                throw new InvalidOperationException("ticket 已被消费（一次性）");
            }

            if (entry.ExpiresAtUtc < DateTime.UtcNow)
            {
                throw new InvalidOperationException("ticket 已过期");
            }

            if (!string.Equals(entry.Service, input.Service, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("service 与签发时不一致");
            }

            if (_tickets.TryUpdate(input.Ticket, entry with { Consumed = true }, entry))
            {
                return new SsoValidateResultDto
                {
                    Message = "ticket 校验成功并已消费",
                    Username = entry.Username,
                    Service = input.Service
                };
            }
        }
    }

    private sealed record TicketEntry(string Username, string Service, DateTime ExpiresAtUtc, bool Consumed);
}
