using Demo.Seckill.Api.Application;
using Demo.Seckill.Api.Dtos;
using Demo.Seckill.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Demo.Seckill.Api.Controllers;

[ApiController]
[Route("api/seckill")]
public sealed class SeckillController(ISeckillAppService seckillAppService) : ControllerBase
{
    [HttpPost("activities")]
    [EndpointName("CreateSeckillActivity")]
    [EndpointSummary("创建秒杀活动与库存")]
    public IActionResult CreateActivityAsync([FromBody] CreateActivityDto input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Name) || input.Stock <= 0)
        {
            return BadRequest(new { message = "请提供 name 与 stock（>0）" });
        }

        var activity = seckillAppService.CreateActivity(input.Name.Trim(), input.Stock);
        return Created($"/api/seckill/activities/{activity.Id}", new
        {
            id = activity.Id,
            name = activity.Name,
            stock = activity.Stock,
            remaining = activity.Remaining,
            createdAt = activity.CreatedAt,
            message = "秒杀活动已创建"
        });
    }

    [HttpGet("activities/{id:guid}")]
    [EndpointName("GetSeckillActivity")]
    [EndpointSummary("查询活动状态与剩余库存")]
    public IActionResult GetActivityAsync(Guid id)
    {
        if (!seckillAppService.TryGetActivity(id, out var activity))
        {
            return NotFound(new { message = "活动不存在", id });
        }

        return Ok(new
        {
            id = activity.Id,
            name = activity.Name,
            stock = activity.Stock,
            remaining = Volatile.Read(ref activity.Remaining),
            successCount = activity.SuccessCount,
            createdAt = activity.CreatedAt
        });
    }

    [HttpPost("{id:guid}/grab")]
    [EndpointName("GrabSeckill")]
    [EndpointSummary("预扣库存（原子），成功进内存队列")]
    public IActionResult GrabAsync(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GrabDto? input)
    {
        var userId = string.IsNullOrWhiteSpace(input?.UserId)
            ? Guid.NewGuid().ToString("N")[..8]
            : input!.UserId.Trim();

        var result = seckillAppService.TryGrab(id, userId);
        return result.Status switch
        {
            GrabStatus.NotFound => NotFound(new { message = "活动不存在", id }),
            GrabStatus.SoldOut => Conflict(new
            {
                message = "库存不足，抢购失败（超卖防护）",
                activityId = id,
                userId,
                remaining = result.Remaining
            }),
            GrabStatus.Success => Ok(new
            {
                message = "预扣库存成功，已进入建单队列",
                activityId = id,
                userId,
                ticketId = result.TicketId,
                remaining = result.Remaining,
                orderStatus = "Queued"
            }),
            _ => StatusCode(500)
        };
    }

    [HttpGet("orders/{ticketId:guid}")]
    [EndpointName("GetSeckillOrder")]
    [EndpointSummary("查询抢购票据/订单状态")]
    public IActionResult GetOrderAsync(Guid ticketId)
    {
        if (!seckillAppService.TryGetOrder(ticketId, out var order))
        {
            return NotFound(new { message = "订单/票据不存在", ticketId });
        }

        return Ok(new
        {
            ticketId = order.TicketId,
            orderId = order.OrderId,
            activityId = order.ActivityId,
            userId = order.UserId,
            status = order.Status.ToString(),
            createdAt = order.CreatedAt,
            builtAt = order.BuiltAt
        });
    }
}
