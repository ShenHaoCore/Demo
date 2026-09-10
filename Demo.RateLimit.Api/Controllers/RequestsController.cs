using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Demo.RateLimit.Api.Controllers;

/// <summary>限流策略对比演示 API。</summary>
[ApiController]
[Route("api/requests")]
public sealed class RequestsController : ControllerBase
{
    [HttpGet("unlimited")]
    public IActionResult GetUnlimitedAsync() =>
        Ok(new
        {
            message = "无限制接口调用成功",
            timestamp = DateTimeOffset.UtcNow
        });

    [HttpGet("fixed")]
    [EnableRateLimiting("fixed")]
    public IActionResult GetFixedAsync() =>
        Ok(new
        {
            message = "固定窗口限流接口调用成功（10 秒内最多 5 次）",
            timestamp = DateTimeOffset.UtcNow
        });

    [HttpGet("sliding")]
    [EnableRateLimiting("sliding")]
    public IActionResult GetSlidingAsync() =>
        Ok(new
        {
            message = "滑动窗口限流接口调用成功（10 秒窗口内最多 5 次）",
            timestamp = DateTimeOffset.UtcNow
        });
}
