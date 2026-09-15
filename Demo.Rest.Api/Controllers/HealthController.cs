using Microsoft.AspNetCore.Mvc;

namespace Demo.Rest.Api.Controllers;

/// <summary>
/// 健康
/// </summary>
[ApiController]
[Tags("健康")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// 检查
    /// </summary>
    /// <returns></returns>
    [HttpGet("/health")]
    [EndpointName("HealthCheck")]
    [EndpointSummary("检查")]
    public IActionResult Get() => Ok(new { status = "ok" });
}
