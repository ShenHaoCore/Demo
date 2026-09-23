using Microsoft.AspNetCore.Mvc;

namespace Demo.I18n.Api.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new { status = "ok", service = "Demo.I18n.Api" });
}
