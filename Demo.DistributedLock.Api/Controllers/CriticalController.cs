using Demo.DistributedLock.Api.Application;
using Demo.DistributedLock.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Demo.DistributedLock.Api.Controllers;

/// <summary>持锁执行临界区演示 API。</summary>
[ApiController]
[Route("api/critical")]
public sealed class CriticalController(ICriticalAppService criticalAppService) : ControllerBase
{
    [HttpPost("{resource}")]
    [EndpointName("CriticalSection")]
    [EndpointSummary("演示持锁执行临界区；并发争用返回 409")]
    public async Task<IActionResult> ExecuteAsync(
        string resource,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CriticalDto? input)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            return BadRequest(new { message = "resource 不能为空" });
        }

        var result = await criticalAppService.ExecuteAsync(resource, input);
        return result.IsConflict ? Conflict(result.Body) : Ok(result.Body);
    }
}
