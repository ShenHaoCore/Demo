using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Versioning.Api.Controllers;

/// <summary>健康检查（版本无关，不进入 OpenAPI 文档）。</summary>
[ApiController]
[ApiVersionNeutral]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new { status = "ok" });
}
