using Demo.Idempotent.Api.Application;
using Demo.Idempotent.Api.Dtos;
using Demo.Idempotent.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Demo.Idempotent.Api.Controllers;

/// <summary>
/// 
/// </summary>
/// <param name="service"></param>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderAppService service) : ControllerBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [Idempotent]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateOrderDto input, CancellationToken cancellationToken)
    {
        if (!IdempotentAttribute.TryGetContext(HttpContext, out var key, out var bodyHash)) { return BadRequest(new { message = "幂等协议未就绪（缺少 Key 或 BodyHash）" }); }
        var outcome = await service.CreateAsync(input, key, bodyHash, cancellationToken);
        return ToActionResult(outcome, key);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="outcome"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private IActionResult ToActionResult(CreateOrderOutcome outcome, string key) => outcome switch
    {
        CreateOrderSuccess success => Created(success.Location, success.Order),
        CreateOrderReplay replay => Replay(replay.Record),
        CreateOrderConflict conflict => Conflict(new { message = conflict.Message, idempotencyKey = key }),
        CreateOrderInvalid invalid => BadRequest(new { message = invalid.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private IActionResult Replay(IdempotencyRecord record)
    {
        object? body = string.IsNullOrEmpty(record.ResponseBodyJson) ? null : JsonSerializer.Deserialize<JsonElement>(record.ResponseBodyJson);
        if (!string.IsNullOrEmpty(record.Location)) { Response.Headers.Location = record.Location; }
        return StatusCode(record.StatusCode, body);
    }
}
