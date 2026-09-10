using Demo.DiLifetime.Api.Application;
using Demo.DiLifetime.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.DiLifetime.Api.Controllers;

[ApiController]
[Route("api/di")]
public sealed class DiDemoController(IDiDemoAppService diDemoAppService) : ControllerBase
{
    [HttpGet("demo")]
    public ActionResult<DiDemoResultDto> GetAsync() =>
        Ok(diDemoAppService.GetDemo(HttpContext.TraceIdentifier));
}
