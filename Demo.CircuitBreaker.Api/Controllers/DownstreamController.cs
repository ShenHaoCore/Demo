using Demo.CircuitBreaker.Api.Application;
using Demo.CircuitBreaker.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.CircuitBreaker.Api.Controllers;

/// <summary>下游模拟与配置。</summary>
[ApiController]
[Route("api/downstream")]
public sealed class DownstreamController(
    IDownstreamAppService downstreamAppService,
    ILogger<DownstreamController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        try
        {
            var body = await downstreamAppService.GetStatusAsync();
            return Ok(body);
        }
        catch (DownstreamException)
        {
            logger.LogWarning("下游接口强制失败");
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { success = false, message = "下游服务故障" });
        }
    }

    [HttpPost("config")]
    public async Task<IActionResult> UpdateConfigAsync([FromBody] DownstreamConfigDto input)
    {
        var body = await downstreamAppService.UpdateConfigAsync(input);
        logger.LogInformation("下游失败开关已更新");
        return Ok(body);
    }
}
