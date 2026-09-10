using Demo.DistributedLock.Api.Dtos;

namespace Demo.DistributedLock.Api.Application;

/// <summary>临界区演示应用服务契约。</summary>
public interface ICriticalAppService
{
    Task<CriticalExecuteResult> ExecuteAsync(string resource, CriticalDto? input);
}

/// <summary>临界区执行结果。</summary>
public sealed class CriticalExecuteResult
{
    public bool IsConflict { get; init; }
    public object Body { get; init; } = default!;
}
