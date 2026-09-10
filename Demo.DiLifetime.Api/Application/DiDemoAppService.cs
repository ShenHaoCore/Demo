using Demo.DiLifetime.Api.Dtos;
using Demo.DiLifetime.Api.Services;

namespace Demo.DiLifetime.Api.Application;

/// <summary>DI 生命周期演示应用服务。</summary>
public sealed class DiDemoAppService(IServiceProvider serviceProvider) : IDiDemoAppService
{
    public DiDemoResultDto GetDemo(string requestId)
    {
        var singleton1 = serviceProvider.GetRequiredService<ISingletonDemoService>();
        var scoped1 = serviceProvider.GetRequiredService<IScopedDemoService>();
        var transient1 = serviceProvider.GetRequiredService<ITransientDemoService>();

        var singleton2 = serviceProvider.GetRequiredService<ISingletonDemoService>();
        var scoped2 = serviceProvider.GetRequiredService<IScopedDemoService>();
        var transient2 = serviceProvider.GetRequiredService<ITransientDemoService>();

        return new DiDemoResultDto
        {
            Message = "依赖注入生命周期演示（同一请求内 Resolve 两次）",
            RequestId = requestId,
            FirstResolve = new DiResolveIdsDto
            {
                Singleton = singleton1.Id,
                Scoped = scoped1.Id,
                Transient = transient1.Id
            },
            SecondResolve = new DiResolveIdsDto
            {
                Singleton = singleton2.Id,
                Scoped = scoped2.Id,
                Transient = transient2.Id
            },
            Explanation = new DiExplanationDto
            {
                Transient = "Transient：每次 Resolve 都新建实例，故同一请求内两次 Id 不同。",
                Scoped = "Scoped：同一请求（同一 scope）共用一个实例，故两次 Id 相同；新请求会换新 Id。",
                Singleton = "Singleton：整个应用进程共用一个实例，故跨请求 Id 也保持相同。"
            }
        };
    }
}
