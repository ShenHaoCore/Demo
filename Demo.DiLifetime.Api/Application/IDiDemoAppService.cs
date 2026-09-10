using Demo.DiLifetime.Api.Dtos;

namespace Demo.DiLifetime.Api.Application;

/// <summary>DI 生命周期演示应用服务契约。</summary>
public interface IDiDemoAppService
{
    DiDemoResultDto GetDemo(string requestId);
}
