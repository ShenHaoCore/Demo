using Demo.Configuration.Api.Application;
using Demo.Configuration.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Configuration.Api.Controllers;

[ApiController]
[Route("api/config")]
public sealed class ConfigController(IConfigAppService configAppService) : ControllerBase
{
    [HttpGet]
    [EndpointName("GetConfigSnapshot")]
    [EndpointSummary("查看当前环境生效配置、各键 ValueSources 与 IOptions 绑定结果")]
    public ActionResult<ConfigSnapshotDto> GetSnapshot() => Ok(configAppService.GetSnapshot());

    [HttpGet("providers")]
    [EndpointName("GetConfigProviders")]
    [EndpointSummary("列出配置提供程序顺序（教学：谁覆盖谁）")]
    public ActionResult<ConfigProvidersResultDto> GetProviders() => Ok(configAppService.GetProviders());
}
