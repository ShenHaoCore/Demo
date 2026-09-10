using System.Text.Json;
using Demo.Payment.Api.Application;
using Demo.Payment.Api.Dtos;
using Demo.Payment.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Payment.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(IPaymentAppService paymentAppService, PaymentOptions options) : ControllerBase
{
    [HttpPost]
    [EndpointName("CreatePayment")]
    [EndpointSummary("创建支付单")]
    public IActionResult CreateAsync([FromBody] CreatePaymentDto input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.OrderId) || input.Amount <= 0)
        {
            return BadRequest(new { message = "请提供 orderId 与 amount（>0）" });
        }

        var payment = paymentAppService.Create(input.OrderId.Trim(), input.Amount);
        return Created($"/api/payments/{payment.Id}", new
        {
            id = payment.Id,
            orderId = payment.OrderId,
            amount = payment.Amount,
            status = payment.Status.ToString(),
            createdAt = payment.CreatedAt,
            message = "支付单已创建，等待回调入账"
        });
    }

    [HttpGet("{id:guid}")]
    [EndpointName("GetPayment")]
    [EndpointSummary("查询支付单状态")]
    public IActionResult GetAsync(Guid id)
    {
        if (!paymentAppService.TryGet(id, out var payment))
        {
            return NotFound(new { message = "支付单不存在", id });
        }

        return Ok(new
        {
            id = payment.Id,
            orderId = payment.OrderId,
            amount = payment.Amount,
            status = payment.Status.ToString(),
            createdAt = payment.CreatedAt,
            paidAt = payment.PaidAt,
            lastNotifyId = payment.LastNotifyId
        });
    }

    [HttpPost("notify")]
    [EndpointName("PaymentNotify")]
    [EndpointSummary("HMAC 签名回调；同一 notifyId 幂等入账一次")]
    public async Task<IActionResult> NotifyAsync()
    {
        using var reader = new StreamReader(Request.Body);
        var bodyText = await reader.ReadToEndAsync();

        NotifyDto? notify;
        try
        {
            notify = JsonSerializer.Deserialize<NotifyDto>(bodyText, JsonDefaults.Options);
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "回调 JSON 无效" });
        }

        if (notify is null ||
            notify.PaymentId == Guid.Empty ||
            string.IsNullOrWhiteSpace(notify.NotifyId) ||
            string.IsNullOrWhiteSpace(notify.Signature))
        {
            return BadRequest(new { message = "需要 paymentId、notifyId、amount、signature" });
        }

        // 签名字段：paymentId|notifyId|amount|status（HMAC-SHA256 hex）
        var amountText = notify.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var payload = $"{notify.PaymentId:D}|{notify.NotifyId}|{amountText}|{notify.Status}";
        var expected = PaymentSecurity.ComputeHmac(payload, options.NotifySecret);
        if (!PaymentSecurity.FixedTimeEqualsHex(expected, notify.Signature))
        {
            return Unauthorized(new { message = "HMAC 签名校验失败" });
        }

        if (!string.Equals(notify.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { message = "非成功状态，忽略入账", received = notify.Status });
        }

        var result = paymentAppService.TryApplyNotify(notify.PaymentId, notify.NotifyId.Trim(), notify.Amount);
        return result.Kind switch
        {
            NotifyKind.NotFound => NotFound(new { message = "支付单不存在", paymentId = notify.PaymentId }),
            NotifyKind.AmountMismatch => Conflict(new
            {
                message = "回调金额与支付单不一致",
                paymentId = notify.PaymentId,
                expected = result.Payment?.Amount,
                actual = notify.Amount
            }),
            NotifyKind.Duplicate => Ok(new
            {
                message = "幂等：同一 notifyId 已入账，忽略重复回调",
                notifyId = notify.NotifyId,
                paymentId = notify.PaymentId,
                status = result.Payment!.Status.ToString(),
                alreadyPaid = true
            }),
            NotifyKind.Applied => Ok(new
            {
                message = "入账成功",
                notifyId = notify.NotifyId,
                paymentId = notify.PaymentId,
                status = result.Payment!.Status.ToString(),
                paidAt = result.Payment.PaidAt
            }),
            NotifyKind.AlreadyPaidOtherNotify => Ok(new
            {
                message = "支付单已入账（其他 notifyId），本回调忽略",
                notifyId = notify.NotifyId,
                paymentId = notify.PaymentId,
                status = result.Payment!.Status.ToString(),
                lastNotifyId = result.Payment.LastNotifyId
            }),
            _ => StatusCode(500)
        };
    }

    [HttpGet("notify/sign-help")]
    [EndpointName("NotifySignHelp")]
    [EndpointSummary("回调签名说明（演示用）")]
    public IActionResult GetSignHelpAsync()
    {
        return Ok(new
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
    }
}
