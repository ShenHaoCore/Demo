using Microsoft.AspNetCore.Mvc;

namespace Demo.JwtAuth.Api.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new { status = "健康", service = "Demo.JwtAuth.Api" });
}
