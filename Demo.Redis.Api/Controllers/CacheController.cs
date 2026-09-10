using Demo.Redis.Api.Application;
using Demo.Redis.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Redis.Api.Controllers;

[ApiController]
[Route("api/cache")]
public sealed class CacheController(IProductAppService productAppService) : ControllerBase
{
    [HttpGet("stats")]
    [EndpointName("GetCacheStats")]
    [EndpointSummary("查看缓存命中/未命中统计")]
    public ActionResult<CacheStatsResultDto> GetStatsAsync() =>
        Ok(productAppService.GetCacheStats());
}
