using Demo.OrderTimeout.Api.Application;
using Demo.OrderTimeout.Api.Dtos;
using Demo.OrderTimeout.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Demo.OrderTimeout.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderAppService orderAppService) : ControllerBase
{
    [HttpPost]
    [EndpointName("CreateOrder")]
    [EndpointSummary("创建待支付订单（可调 timeoutSeconds）")]
    public IActionResult CreateAsync([FromBody] CreateOrderDto input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Product) || input.Amount <= 0)
        {
            return BadRequest(new { message = "请提供 product 与 amount（>0）" });
        }

        // 默认可调短超时便于演示（秒）
        var timeoutSeconds = input.TimeoutSeconds is > 0 ? input.TimeoutSeconds.Value : 15;
        timeoutSeconds = Math.Clamp(timeoutSeconds, 3, 3600);

        var order = orderAppService.Create(input.Product.Trim(), input.Amount, TimeSpan.FromSeconds(timeoutSeconds));
        return Created($"/api/orders/{order.Id}", new
        {
            id = order.Id,
            product = order.Product,
            amount = order.Amount,
            status = order.Status.ToString(),
            createdAt = order.CreatedAt,
            expireAt = order.ExpireAt,
            timeoutSeconds,
            message = "待支付订单已创建，超时将自动关单"
        });
    }

    [HttpGet("{id:guid}")]
    [EndpointName("GetOrder")]
    [EndpointSummary("查询订单状态")]
    public IActionResult GetAsync(Guid id)
    {
        if (!orderAppService.TryGet(id, out var order))
        {
            return NotFound(new { message = "订单不存在", id });
        }

        return Ok(ToDto(order));
    }

    [HttpPost("{id:guid}/pay")]
    [EndpointName("PayOrder")]
    [EndpointSummary("支付订单；与关单用状态 CAS 竞态")]
    public IActionResult PayAsync(Guid id)
    {
        var result = orderAppService.TryPay(id);
        return result.Status switch
        {
            TransitionStatus.NotFound => NotFound(new { message = "订单不存在", id }),
            TransitionStatus.Conflict => Conflict(new
            {
                message = "支付失败：订单已非待支付状态（可能已被超时关单）",
                id,
                currentStatus = result.Order?.Status.ToString()
            }),
            TransitionStatus.Success => Ok(new
            {
                message = "支付成功（CAS：Pending -> Paid）",
                order = ToDto(result.Order!)
            }),
            _ => StatusCode(500)
        };
    }

    [HttpPost("{id:guid}/close")]
    [EndpointName("CloseOrder")]
    [EndpointSummary("手动关单（同样使用 CAS）")]
    public IActionResult CloseAsync(Guid id)
    {
        var result = orderAppService.TryClose(id, "手动关单");
        return result.Status switch
        {
            TransitionStatus.NotFound => NotFound(new { message = "订单不存在", id }),
            TransitionStatus.Conflict => Conflict(new
            {
                message = "关单失败：订单已非待支付状态（可能已支付）",
                id,
                currentStatus = result.Order?.Status.ToString()
            }),
            TransitionStatus.Success => Ok(new
            {
                message = "关单成功（CAS：Pending -> Closed）",
                order = ToDto(result.Order!)
            }),
            _ => StatusCode(500)
        };
    }

    private static object ToDto(Order order) => new
    {
        id = order.Id,
        product = order.Product,
        amount = order.Amount,
        status = order.Status.ToString(),
        createdAt = order.CreatedAt,
        expireAt = order.ExpireAt,
        paidAt = order.PaidAt,
        closedAt = order.ClosedAt,
        closeReason = order.CloseReason
    };
}
