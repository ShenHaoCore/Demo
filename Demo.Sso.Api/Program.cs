using System.Collections.Concurrent;
using System.Security.Cryptography;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<SsoStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "健康", service = "Demo.Sso.Api" }));

app.MapGet("/api/sso/clients", (SsoStore store) =>
    Results.Ok(new
    {
        message = "演示用假客户端（service）",
        clients = store.Clients.Select(c => new { c.Name, service = c.ServiceUrl })
    }));

app.MapPost("/api/sso/login", (LoginRequest request, SsoStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "用户名和密码不能为空" });

    var session = store.Login(request.Username.Trim());
    return Results.Ok(new
    {
        message = "SSO 登录成功（演示：任意非空密码均可）",
        ssoSessionId = session,
        username = request.Username.Trim(),
        hint = "随后用 session 向 /api/sso/ticket?service= 换取一次性 ticket"
    });
});

app.MapGet("/api/sso/ticket", (string? service, string? ssoSessionId, SsoStore store) =>
{
    if (string.IsNullOrWhiteSpace(service))
        return Results.BadRequest(new { message = "必须提供 service 参数" });

    if (string.IsNullOrWhiteSpace(ssoSessionId))
        return Results.BadRequest(new { message = "必须提供 ssoSessionId（演示简化：用 query 传递）" });

    if (!store.TryIssueTicket(ssoSessionId, service, out var ticket, out var error))
        return Results.BadRequest(new { message = error });

    return Results.Ok(new
    {
        message = "已签发一次性 CAS 风格 ticket",
        ticket,
        service,
        hint = "客户端应携带 ticket 调用 /api/sso/validate 完成校验，ticket 仅能使用一次"
    });
});

app.MapPost("/api/sso/validate", (ValidateRequest request, SsoStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Ticket) || string.IsNullOrWhiteSpace(request.Service))
        return Results.BadRequest(new { message = "ticket 与 service 均不能为空" });

    if (!store.TryValidateAndConsume(request.Ticket, request.Service, out var username, out var error))
        return Results.BadRequest(new { message = error });

    return Results.Ok(new
    {
        message = "ticket 校验成功并已消费",
        username,
        service = request.Service
    });
});

app.Run();

record LoginRequest(string Username, string Password);
record ValidateRequest(string Ticket, string Service);

sealed record SsoClient(string Name, string ServiceUrl);

sealed class SsoStore
{
    public IReadOnlyList<SsoClient> Clients { get; } =
    [
        new("假客户端 A - 门户", "https://app-a.demo.local/callback"),
        new("假客户端 B - 后台", "https://app-b.demo.local/callback")
    ];

    private readonly ConcurrentDictionary<string, string> _sessions = new(); // sessionId -> username
    private readonly ConcurrentDictionary<string, TicketEntry> _tickets = new();

    public string Login(string username)
    {
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _sessions[sessionId] = username;
        return sessionId;
    }

    public bool TryIssueTicket(string ssoSessionId, string service, out string? ticket, out string? error)
    {
        ticket = null;
        error = null;

        if (!_sessions.TryGetValue(ssoSessionId, out var username))
        {
            error = "SSO 会话无效，请先登录";
            return false;
        }

        if (Clients.All(c => !string.Equals(c.ServiceUrl, service, StringComparison.OrdinalIgnoreCase)))
        {
            error = $"未知的 service，仅支持演示客户端：{string.Join("、", Clients.Select(c => c.ServiceUrl))}";
            return false;
        }

        ticket = "ST-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        _tickets[ticket] = new TicketEntry(username, service, DateTime.UtcNow.AddMinutes(5), Consumed: false);
        return true;
    }

    public bool TryValidateAndConsume(string ticket, string service, out string? username, out string? error)
    {
        username = null;
        error = null;

        while (true)
        {
            if (!_tickets.TryGetValue(ticket, out var entry))
            {
                error = "ticket 无效";
                return false;
            }

            if (entry.Consumed)
            {
                error = "ticket 已被消费（一次性）";
                return false;
            }

            if (entry.ExpiresAtUtc < DateTime.UtcNow)
            {
                error = "ticket 已过期";
                return false;
            }

            if (!string.Equals(entry.Service, service, StringComparison.OrdinalIgnoreCase))
            {
                error = "service 与签发时不一致";
                return false;
            }

            // CAS 消费：仅一人能将 Consumed 从 false 改为 true
            if (_tickets.TryUpdate(ticket, entry with { Consumed = true }, entry))
            {
                username = entry.Username;
                return true;
            }
        }
    }

    private sealed record TicketEntry(string Username, string Service, DateTime ExpiresAtUtc, bool Consumed);
}
