using Demo.Rest.Api.Application;
using Demo.Rest.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Rest.Api.Controllers;

/// <summary>
/// 订单
/// </summary>
/// <param name="service">订单应用服务</param>
/// <param name="logger">日志记录器</param>
[ApiController]
[Route("api/orders")]
[Tags("订单")]
public sealed class OrdersController(IOrderAppService service, ILogger<OrdersController> logger) : ControllerBase
{
    /// <summary>
    /// 列表
    /// </summary>
    /// <returns>订单列表</returns>
    [HttpGet]
    [EndpointName("GetOrderList")]
    [EndpointSummary("列表")]
    public async Task<IActionResult> GetListAsync() => Ok(await service.GetListAsync());

    /// <summary>
    /// 详情
    /// </summary>
    /// <param name="id">订单唯一标识</param>
    /// <returns>订单详情</returns>
    [HttpGet("{id:guid}")]
    [EndpointName("GetOrder")]
    [EndpointSummary("详情")]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        var order = await service.GetAsync(id);
        if (order is null) { return NotFound(new { message = $"未找到订单：{id}" }); }
        return Ok(order);
    }

    /// <summary>
    /// 创建
    /// </summary>
    /// <param name="input">创建订单输入</param>
    /// <returns>创建的订单</returns>
    [HttpPost]
    [EndpointName("CreateOrder")]
    [EndpointSummary("创建")]
    public async Task<IActionResult> CreateAsync(CreateOrderDto input)
    {
        try
        {
            var order = await service.CreateAsync(input);
            logger.LogInformation("已创建订单 {OrderId}，客户：{CustomerName}", order.Id, order.CustomerName);
            return Created($"/api/orders/{order.Id}", order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 全量更新
    /// </summary>
    /// <param name="id">订单唯一标识</param>
    /// <param name="input">更新订单输入</param>
    /// <returns>更新的订单</returns>
    [HttpPut("{id:guid}")]
    [EndpointName("UpdateOrder")]
    [EndpointSummary("全量更新")]
    public async Task<IActionResult> UpdateAsync(Guid id, UpdateOrderDto input)
    {
        try
        {
            var updated = await service.UpdateAsync(id, input);
            if (updated is null) { return NotFound(new { message = $"未找到订单：{id}" }); }
            logger.LogInformation("已全量更新订单 {OrderId}", id);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 部分更新
    /// </summary>
    /// <param name="id">订单唯一标识</param>
    /// <param name="input">部分更新订单输入</param>
    /// <returns>更新的订单</returns>
    [HttpPatch("{id:guid}")]
    [EndpointName("PatchOrder")]
    [EndpointSummary("部分更新")]
    public async Task<IActionResult> PatchAsync(Guid id, PatchOrderDto input)
    {
        try
        {
            var updated = await service.PatchAsync(id, input);
            if (updated is null) { return NotFound(new { message = $"未找到订单：{id}" }); }
            logger.LogInformation("已部分更新订单 {OrderId}", id);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id">订单唯一标识</param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    [EndpointName("DeleteOrder")]
    [EndpointSummary("删除")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        if (!await service.DeleteAsync(id)) { return NotFound(new { message = $"未找到订单：{id}" }); }
        logger.LogInformation("已删除订单 {OrderId}", id);
        return NoContent();
    }
}
