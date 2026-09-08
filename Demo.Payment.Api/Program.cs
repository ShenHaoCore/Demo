using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<PaymentStore>();

// 演示用密钥，可通过配置覆盖
var notifySecret = builder.Configuration["Payment:NotifySecret"] ?? "demo-payment-secret";
builder.Services.AddSingleton(new PaymentOptions(notifySecret));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.Payment.Api" }));

app.MapPost("/api/payments", (CreatePaymentRequest request, PaymentStore store) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.OrderId) || request.Amount <= 0)
    {
        return Results.BadRequest(new { message = "请提供 orderId 与 amount（>0）" });
    }

    var payment = store.Create(request.OrderId.Trim(), request.Amount);
    return Results.Created($"/api/payments/{payment.Id}", new
    {
        id = payment.Id,
        orderId = payment.OrderId,
        amount = payment.Amount,
        status = payment.Status.ToString(),
        createdAt = payment.CreatedAt,
        message = "支付单已创建，等待回调入账"
    });
})
.WithName("CreatePayment")
.WithSummary("创建支付单");

app.MapGet("/api/payments/{id:guid}", (Guid id, PaymentStore store) =>
{
    if (!store.TryGet(id, out var payment))
    {
        return Results.NotFound(new { message = "支付单不存在", id });
    }

    return Results.Ok(new
    {
        id = payment.Id,
        orderId = payment.OrderId,
        amount = payment.Amount,
        status = payment.Status.ToString(),
        createdAt = payment.CreatedAt,
        paidAt = payment.PaidAt,
        lastNotifyId = payment.LastNotifyId
    });
})
.WithName("GetPayment")
.WithSummary("查询支付单状态");

app.MapPost("/api/payments/notify", async (HttpRequest http, PaymentStore store, PaymentOptions options) =>
{
    using var reader = new StreamReader(http.Body);
    var bodyText = await reader.ReadToEndAsync();

    NotifyRequest? notify;
    try
    {
        notify = JsonSerializer.Deserialize<NotifyRequest>(bodyText, JsonDefaults.Options);
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { message = "回调 JSON 无效" });
    }

    if (notify is null ||
        notify.PaymentId == Guid.Empty ||
        string.IsNullOrWhiteSpace(notify.NotifyId) ||
        string.IsNullOrWhiteSpace(notify.Signature))
    {
        return Results.BadRequest(new { message = "需要 paymentId、notifyId、amount、signature" });
    }

    // 签名字段：paymentId|notifyId|amount|status（HMAC-SHA256 hex）
    var amountText = notify.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    var payload = $"{notify.PaymentId:D}|{notify.NotifyId}|{amountText}|{notify.Status}";
    var expected = ComputeHmac(payload, options.NotifySecret);
    if (!string.Equals(expected, notify.Signature.Trim(), StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(new { message = "HMAC 签名校验失败" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    if (!string.Equals(notify.Status, "success", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Ok(new { message = "非成功状态，忽略入账", received = notify.Status });
    }

    var result = store.TryApplyNotify(notify.PaymentId, notify.NotifyId.Trim(), notify.Amount);
    return result.Kind switch
    {
        NotifyKind.NotFound => Results.NotFound(new { message = "支付单不存在", paymentId = notify.PaymentId }),
        NotifyKind.AmountMismatch => Results.Conflict(new
        {
            message = "回调金额与支付单不一致",
            paymentId = notify.PaymentId,
            expected = result.Payment?.Amount,
            actual = notify.Amount
        }),
        NotifyKind.Duplicate => Results.Ok(new
        {
            message = "幂等：同一 notifyId 已入账，忽略重复回调",
            notifyId = notify.NotifyId,
            paymentId = notify.PaymentId,
            status = result.Payment!.Status.ToString(),
            alreadyPaid = true
        }),
        NotifyKind.Applied => Results.Ok(new
        {
            message = "入账成功",
            notifyId = notify.NotifyId,
            paymentId = notify.PaymentId,
            status = result.Payment!.Status.ToString(),
            paidAt = result.Payment.PaidAt
        }),
        NotifyKind.AlreadyPaidOtherNotify => Results.Ok(new
        {
            message = "支付单已入账（其他 notifyId），本回调忽略",
            notifyId = notify.NotifyId,
            paymentId = notify.PaymentId,
            status = result.Payment!.Status.ToString(),
            lastNotifyId = result.Payment.LastNotifyId
        }),
        _ => Results.StatusCode(500)
    };
})
.WithName("PaymentNotify")
.WithSummary("HMAC 签名回调；同一 notifyId 幂等入账一次");

app.MapGet("/api/payments/notify/sign-help", (PaymentOptions options) =>
{
    return Results.Ok(new
    {
        algorithm = "HMAC-SHA256",
        payloadFormat = "paymentId|notifyId|amount|status",
        amountFormat = "与 JSON 中 amount 的默认小数文本一致（如 10.5）",
        secretHint = "配置项 Payment:NotifySecret，默认 demo-payment-secret",
        example = new
        {
            paymentId = "00000000-0000-0000-0000-000000000001",
            notifyId = "n-001",
            amount = 10.5m,
            status = "success",
            payload = "00000000-0000-0000-0000-000000000001|n-001|10.5|success",
            howToSign = "HMACSHA256(UTF8(payload), UTF8(secret)) -> hex"
        },
        secretConfigured = !string.IsNullOrEmpty(options.NotifySecret)
    });
})
.WithName("NotifySignHelp")
.WithSummary("回调签名说明（演示用）");

app.Run();

static string ComputeHmac(string payload, string secret)
{
    var key = Encoding.UTF8.GetBytes(secret);
    var data = Encoding.UTF8.GetBytes(payload);
    var hash = HMACSHA256.HashData(key, data);
    return Convert.ToHexString(hash);
}

static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}

sealed class PaymentStore
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();
    private readonly ConcurrentDictionary<string, Guid> _notifyIndex = new(StringComparer.Ordinal);

    public Payment Create(string orderId, decimal amount)
    {
        var payment = new Payment(
            Id: Guid.NewGuid(),
            OrderId: orderId,
            Amount: amount,
            Status: PaymentStatus.Pending,
            CreatedAt: DateTimeOffset.UtcNow,
            PaidAt: null,
            LastNotifyId: null);

        _payments[payment.Id] = payment;
        return payment;
    }

    public bool TryGet(Guid id, out Payment payment) =>
        _payments.TryGetValue(id, out payment!);

    public NotifyResult TryApplyNotify(Guid paymentId, string notifyId, decimal amount)
    {
        // 幂等：同一 notifyId 只入账一次
        if (_notifyIndex.TryGetValue(notifyId, out var existingPaymentId))
        {
            _payments.TryGetValue(existingPaymentId, out var existing);
            return new NotifyResult(NotifyKind.Duplicate, existing);
        }

        while (true)
        {
            if (!_payments.TryGetValue(paymentId, out var current))
            {
                return new NotifyResult(NotifyKind.NotFound, null);
            }

            if (current.Amount != amount)
            {
                return new NotifyResult(NotifyKind.AmountMismatch, current);
            }

            if (current.Status == PaymentStatus.Paid)
            {
                return new NotifyResult(NotifyKind.AlreadyPaidOtherNotify, current);
            }

            var updated = current with
            {
                Status = PaymentStatus.Paid,
                PaidAt = DateTimeOffset.UtcNow,
                LastNotifyId = notifyId
            };

            if (!_payments.TryUpdate(paymentId, updated, current))
            {
                continue;
            }

            if (!_notifyIndex.TryAdd(notifyId, paymentId))
            {
                // 极端并发：另一请求已用同 notifyId；回滚本单状态需谨慎，这里以 notify 索引为准
                _payments.TryGetValue(_notifyIndex[notifyId], out var dup);
                return new NotifyResult(NotifyKind.Duplicate, dup);
            }

            return new NotifyResult(NotifyKind.Applied, updated);
        }
    }
}

enum PaymentStatus
{
    Pending,
    Paid
}

enum NotifyKind
{
    Applied,
    Duplicate,
    AlreadyPaidOtherNotify,
    AmountMismatch,
    NotFound
}

record Payment(
    Guid Id,
    string OrderId,
    decimal Amount,
    PaymentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    string? LastNotifyId);

record CreatePaymentRequest(string OrderId, decimal Amount);

record NotifyRequest(Guid PaymentId, string NotifyId, decimal Amount, string Status, string Signature);

record NotifyResult(NotifyKind Kind, Payment? Payment);

record PaymentOptions(string NotifySecret);
