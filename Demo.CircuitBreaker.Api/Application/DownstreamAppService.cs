using Demo.CircuitBreaker.Api.Dtos;

namespace Demo.CircuitBreaker.Api.Application;

/// <summary>下游模拟应用服务。</summary>
public sealed class DownstreamAppService(DownstreamOptions options) : IDownstreamAppService
{
    public Task<object> GetStatusAsync()
    {
        if (options.ForceFail)
        {
            throw new DownstreamException("下游服务故障");
        }

        return Task.FromResult<object>(new
        {
            success = true,
            message = "下游服务正常",
            timestamp = DateTimeOffset.UtcNow
        });
    }

    public Task<object> UpdateConfigAsync(DownstreamConfigDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        options.ForceFail = input.ForceFail;
        return Task.FromResult<object>(new { options.ForceFail, message = "下游配置已更新" });
    }
}
