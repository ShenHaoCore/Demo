using Demo.Inbox.Api.Application;
using Demo.Inbox.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Inbox.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(IInboxAppService inboxAppService) : ControllerBase
{
    [HttpPost]
    [EndpointName("ReceiveEvent")]
    [EndpointSummary("接收事件（按 messageId 去重）；?fail=true 模拟失败")]
    public IActionResult ReceiveAsync([FromBody] ReceiveEventDto input, [FromQuery] bool? fail)
    {
        if (string.IsNullOrWhiteSpace(input.MessageId))
        {
            return BadRequest(new { message = "messageId 不能为空" });
        }

        var messageId = input.MessageId.Trim();

        // 已处理过：直接返回 200，不重复执行副作用
        if (inboxAppService.IsProcessed(messageId))
        {
            inboxAppService.IncrementDedup();
            return Ok(new
            {
                message = "消息已处理过（inbox 去重）",
                messageId,
                duplicated = true,
                stats = inboxAppService.GetStats()
            });
        }

        // 模拟消费失败以便演示重投：不写入 inbox，下次可再次投递
        if (fail == true)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "模拟处理失败，未写入 inbox，可重新投递",
                messageId,
                failed = true
            });
        }

        var processed = inboxAppService.TryProcess(messageId, input.Payload, out var alreadyProcessed);
        if (alreadyProcessed)
        {
            inboxAppService.IncrementDedup();
            return Ok(new
            {
                message = "消息已处理过（inbox 去重）",
                messageId,
                duplicated = true,
                stats = inboxAppService.GetStats()
            });
        }

        if (!processed)
        {
            return Conflict(new { message = "消息正在处理中，请稍后重试", messageId });
        }

        return Ok(new
        {
            message = "事件已处理，副作用已执行（计数 +1）",
            messageId,
            duplicated = false,
            stats = inboxAppService.GetStats()
        });
    }
}
