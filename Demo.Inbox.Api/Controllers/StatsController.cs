using Demo.Inbox.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Inbox.Api.Controllers;

[ApiController]
[Route("api/stats")]
public sealed class StatsController(IInboxAppService inboxAppService) : ControllerBase
{
    [HttpGet]
    [EndpointName("GetStats")]
    [EndpointSummary("查看处理次数与去重次数")]
    public IActionResult GetAsync() => Ok(inboxAppService.GetStats());
}
