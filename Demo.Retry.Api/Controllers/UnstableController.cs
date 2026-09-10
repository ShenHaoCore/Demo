using Demo.Retry.Api.Application;
using Demo.Retry.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Retry.Api.Controllers;

/// <summary>不稳定下游与 Polly 重试代理演示 API。</summary>
[ApiController]
[Route("api")]
public sealed class UnstableController(IRetryDemoAppService retryDemoAppService) : ControllerBase
{
    [HttpGet("unstable")]
    public async Task<IActionResult> GetUnstableAsync([FromQuery] double? rate)
    {
        var result = await retryDemoAppService.InvokeUnstableAsync(rate);
        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result.Body);
        }

        return Ok(result.Body);
    }

    [HttpPost("unstable/config")]
    public async Task<IActionResult> UpdateConfigAsync([FromBody] UnstableConfigDto input)
    {
        try
        {
            var body = await retryDemoAppService.UpdateConfigAsync(input);
            return Ok(body);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("proxy")]
    public async Task<IActionResult> GetProxyAsync()
    {
        var body = await retryDemoAppService.ProxyWithRetryAsync();
        return Ok(body);
    }
}
