using Demo.Outbox.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Outbox.Api.Controllers;

[ApiController]
[Route("api/outbox")]
public sealed class OutboxController(IOutboxAppService outboxAppService) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListOutbox")]
    [EndpointSummary("查看 outbox 消息状态")]
    public async Task<IActionResult> GetListAsync() => Ok(await outboxAppService.GetMessagesAsync());
}
