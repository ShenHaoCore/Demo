using Demo.CircuitBreaker.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace Demo.CircuitBreaker.Api.Controllers;

/// <summary>经熔断器调用下游。</summary>
[ApiController]
[Route("api/call")]
public sealed class CallController(
    ICallAppService callAppService,
    ICircuitBreakerAppService circuitBreakerAppService,
    ILogger<CallController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        try
        {
            var body = await callAppService.CallDownstreamAsync();
            return Ok(body);
        }
        catch (CircuitOpenException ex)
        {
            logger.LogWarning("熔断器打开，拒绝调用：{Message}", ex.Message);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    success = false,
                    message = ex.Message,
                    breaker = await circuitBreakerAppService.GetStatusAsync()
                });
        }
        catch (DownstreamException ex)
        {
            logger.LogWarning("下游调用失败：{Message}", ex.Message);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    success = false,
                    message = ex.Message,
                    breaker = await circuitBreakerAppService.GetStatusAsync()
                });
        }
    }
}
