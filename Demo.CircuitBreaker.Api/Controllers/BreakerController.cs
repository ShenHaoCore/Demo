using Demo.CircuitBreaker.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace Demo.CircuitBreaker.Api.Controllers;

/// <summary>熔断器状态与重置。</summary>
[ApiController]
[Route("api/breaker")]
public sealed class BreakerController(
    ICircuitBreakerAppService circuitBreakerAppService,
    ILogger<BreakerController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync() => Ok(await circuitBreakerAppService.GetStatusAsync());

    [HttpPost("reset")]
    public async Task<IActionResult> ResetAsync()
    {
        var body = await circuitBreakerAppService.ResetAsync();
        logger.LogInformation("熔断器已手动重置为 Closed");
        return Ok(body);
    }
}
