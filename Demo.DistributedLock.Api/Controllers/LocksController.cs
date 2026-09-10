using Demo.DistributedLock.Api.Application;
using Demo.DistributedLock.Api.Dtos;
using Demo.DistributedLock.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Demo.DistributedLock.Api.Controllers;

/// <summary>分布式锁管理 API。</summary>
[ApiController]
[Route("api/locks")]
public sealed class LocksController(ILockAppService lockAppService) : ControllerBase
{
    [HttpPost("{resource}/acquire")]
    [EndpointName("AcquireLock")]
    [EndpointSummary("获取分布式锁（SET NX EX）")]
    public async Task<IActionResult> AcquireAsync(
        string resource,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AcquireLockDto? input)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            return BadRequest(new { message = "resource 不能为空" });
        }

        var result = await lockAppService.AcquireAsync(resource, input);
        return result.Success ? Ok(result.Body) : Conflict(result.Body);
    }

    [HttpPost("{resource}/release")]
    [EndpointName("ReleaseLock")]
    [EndpointSummary("带 token 释放锁；错误 token 拒绝")]
    public async Task<IActionResult> ReleaseAsync(string resource, [FromBody] ReleaseLockDto input)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            return BadRequest(new { message = "resource 不能为空" });
        }

        if (input is null || string.IsNullOrWhiteSpace(input.Token))
        {
            return BadRequest(new { message = "必须提供 token" });
        }

        var result = await lockAppService.ReleaseAsync(resource, input);
        return result.Status switch
        {
            ReleaseStatus.Released => Ok(result.Body),
            ReleaseStatus.NotHeld => NotFound(result.Body),
            ReleaseStatus.TokenMismatch => Conflict(result.Body),
            _ => StatusCode(500)
        };
    }

    [HttpGet("{resource}")]
    [EndpointName("GetLockInfo")]
    [EndpointSummary("查看资源锁状态")]
    public async Task<IActionResult> GetAsync(string resource)
    {
        var body = await lockAppService.GetInfoAsync(resource);
        return Ok(body);
    }
}
