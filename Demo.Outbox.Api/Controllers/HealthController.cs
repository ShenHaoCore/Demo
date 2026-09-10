using Microsoft.AspNetCore.Mvc;

namespace Demo.Outbox.Api.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new { status = "ok", service = "Demo.Outbox.Api" });
}
