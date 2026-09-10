using Microsoft.AspNetCore.Mvc;

namespace Demo.Idempotent.Api.Controllers;

/// <summary>
/// 
/// </summary>
[ApiController]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new { status = "ok", service = "Demo.Idempotent.Api" });
}