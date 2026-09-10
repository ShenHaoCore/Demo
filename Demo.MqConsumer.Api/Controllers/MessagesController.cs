using Demo.MqConsumer.Api.Application;
using Demo.MqConsumer.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.MqConsumer.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class MessagesController(IMessageAppService messageAppService) : ControllerBase
{
    [HttpPost("messages")]
    [EndpointName("PublishMessage")]
    [EndpointSummary("投递消息到内存 Broker（failUntilAttempt 可模拟前 N 次处理失败）")]
    public IActionResult CreateAsync([FromBody] PublishMessageDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Payload))
        {
            return BadRequest(new { message = "payload 不能为空" });
        }

        var queued = messageAppService.Enqueue(input.Payload.Trim(), input.FailUntilAttempt ?? 0);
        return Accepted("/api/queue", new
        {
            info = "消息已投递到内存队列",
            message = queued
        });
    }

    [HttpGet("queue")]
    [EndpointName("GetQueue")]
    [EndpointSummary("查看队列中的消息")]
    public IActionResult GetQueueAsync() => Ok(messageAppService.GetQueueSnapshot());

    [HttpGet("dlq")]
    [EndpointName("GetDlq")]
    [EndpointSummary("查看死信队列")]
    public IActionResult GetDlqAsync() => Ok(messageAppService.GetDlqSnapshot());
}
