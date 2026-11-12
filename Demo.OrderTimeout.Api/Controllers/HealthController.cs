using Demo.OrderTimeout.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace Demo.OrderTimeout.Api.Controllers;

[ApiController]
public sealed class HealthController(IOrderAppService orderAppService) : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "Demo.OrderTimeout.Api",
        scheduler = orderAppService.SchedulerProvider
    });
}
