using Microsoft.AspNetCore.Mvc;

namespace Demo.PasswordAuth.Api.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new { status = "健康", service = "Demo.PasswordAuth.Api" });
}