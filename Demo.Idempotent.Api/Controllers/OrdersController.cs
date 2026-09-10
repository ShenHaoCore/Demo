using Demo.Idempotent.Api.Application.Idempotency;
using Demo.Idempotent.Api.Application.Orders;
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
        var result = await service.CreateAsync(input, key, bodyHash, cancellationToken);
        return ToActionResult(result, key);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private IActionResult ToActionResult(CreateOrderResult result, string key) => result switch
    {
        CreateOrderSuccessResult success => Created(success.Location, success.Order),
        CreateOrderReplayResult replay => Replay(replay.Snapshot),
        CreateOrderConflictResult conflict => Conflict(new { message = conflict.Message, idempotencyKey = key }),
        CreateOrderInvalidResult invalid => BadRequest(new { message = invalid.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="snapshot"></param>
    /// <returns></returns>
    private IActionResult Replay(IdempotencySnapshot snapshot)
    {
        object? body = string.IsNullOrEmpty(snapshot.ResponseBodyJson) ? null : JsonSerializer.Deserialize<JsonElement>(snapshot.ResponseBodyJson);
        if (!string.IsNullOrEmpty(snapshot.Location)) { Response.Headers.Location = snapshot.Location; }
        return StatusCode(snapshot.StatusCode, body);
    }
}
