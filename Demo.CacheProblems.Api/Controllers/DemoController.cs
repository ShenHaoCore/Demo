using Demo.CacheProblems.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace Demo.CacheProblems.Api.Controllers;

/// <summary>缓存穿透 / 击穿 / 雪崩演示 API。</summary>
[ApiController]
[Route("api/demo")]
public sealed class DemoController(ICacheDemoAppService cacheDemoAppService) : ControllerBase
{
    [HttpGet("penetration")]
    [EndpointName("DemoPenetration")]
    [EndpointSummary("缓存穿透：不存在也缓存空值（可用 cacheNull 对比）")]
    public async Task<IActionResult> GetPenetrationAsync([FromQuery] string? id, [FromQuery] bool? cacheNull)
    {
        var body = await cacheDemoAppService.GetPenetrationAsync(id, cacheNull);
        return Ok(body);
    }

    [HttpGet("breakdown")]
    [EndpointName("DemoBreakdown")]
    [EndpointSummary("缓存击穿：热点过期时用锁互斥重建（可用 useLock 对比）")]
    public async Task<IActionResult> GetBreakdownAsync([FromQuery] bool? useLock)
    {
        var result = await cacheDemoAppService.GetBreakdownAsync(useLock);
        if (result.IsConflict)
        {
            return Conflict(result.Body);
        }

        return Ok(result.Body);
    }

    [HttpGet("avalanche")]
    [EndpointName("DemoAvalanche")]
    [EndpointSummary("缓存雪崩：过期时间加随机抖动（可用 jitter 对比）")]
    public async Task<IActionResult> GetAvalancheAsync([FromQuery] bool? jitter, [FromQuery] int? baseSeconds)
    {
        var body = await cacheDemoAppService.GetAvalancheAsync(jitter, baseSeconds);
        return Ok(body);
    }

    [HttpPost("cache/expire-hot")]
    [EndpointName("ExpireHotKey")]
    [EndpointSummary("手动过期热点 key，便于演示击穿")]
    public async Task<IActionResult> ExpireHotKeyAsync()
    {
        await cacheDemoAppService.ExpireHotKeyAsync();
        return Ok(new { message = "已手动过期热点 key：hot:product，便于演示击穿" });
    }
}
