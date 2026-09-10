using Demo.Outbox.Api.Application;
using Demo.Outbox.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Outbox.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOutboxAppService outboxAppService) : ControllerBase
{
    [HttpPost]
    [EndpointName("CreateOrder")]
    [EndpointSummary("创建订单并同事务写入 outbox")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateOrderDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Product) || input.Quantity <= 0)
        {
            return BadRequest(new { message = "请提供有效的 product 与 quantity（>0）" });
        }

        try
        {
            var (order, message) = await outboxAppService.CreateOrderWithOutboxAsync(input.Product.Trim(), input.Quantity);
            return Created($"/api/orders/{order.Id}", new
            {
                message = "订单与 outbox 消息已在同一内存事务中写入",
                order,
                outboxMessageId = message.Id
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    [EndpointName("ListOrders")]
    public async Task<IActionResult> GetListAsync() => Ok(await outboxAppService.GetOrdersAsync());
}
