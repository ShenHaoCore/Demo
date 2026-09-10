using Demo.Retry.Api.Dtos;

namespace Demo.Retry.Api.Application;

/// <summary>重试演示应用服务契约。</summary>
public interface IRetryDemoAppService
{
    Task<UnstableInvokeResult> InvokeUnstableAsync(double? rate);

    Task<object> UpdateConfigAsync(UnstableConfigDto input);

    Task<object> ProxyWithRetryAsync();
}

/// <summary>不稳定接口调用结果（含 HTTP 成功与否）。</summary>
public sealed class UnstableInvokeResult
{
    public bool IsSuccess { get; init; }
    public UnstableResultDto Body { get; init; } = default!;
}
