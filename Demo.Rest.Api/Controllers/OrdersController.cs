using Demo.Rest.Api.Application;
using Demo.Rest.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Rest.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderAppService _orderAppService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderAppService orderAppService, ILogger<OrdersController> logger)
    {
        _orderAppService = orderAppService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetListAsync() => Ok(await _orderAppService.GetListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        var order = await _orderAppService.GetAsync(id);
        if (order is null)
        {
            return NotFound(new { message = $"未找到订单：{id}" });
        }

        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateOrderDto input)
    {
        try
        {
            var order = await _orderAppService.CreateAsync(input);
            _logger.LogInformation("已创建订单 {OrderId}，客户：{CustomerName}", order.Id, order.CustomerName);
            return Created($"/api/orders/{order.Id}", order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT 直接覆盖（last-write-wins）。并发控制见 Demo.ETag.Api / Demo.OptimisticLock.Api
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, UpdateOrderDto input)
    {
        try
        {
            var updated = await _orderAppService.UpdateAsync(id, input);
            if (updated is null)
            {
                return NotFound(new { message = $"未找到订单：{id}" });
            }

            _logger.LogInformation("已全量更新订单 {OrderId}", id);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> PatchAsync(Guid id, PatchOrderDto input)
    {
        try
        {
            var updated = await _orderAppService.PatchAsync(id, input);
            if (updated is null)
            {
                return NotFound(new { message = $"未找到订单：{id}" });
            }

            _logger.LogInformation("已部分更新订单 {OrderId}", id);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        if (!await _orderAppService.DeleteAsync(id))
        {
            return NotFound(new { message = $"未找到订单：{id}" });
        }

        _logger.LogInformation("已删除订单 {OrderId}", id);
        return NoContent();
    }
}
