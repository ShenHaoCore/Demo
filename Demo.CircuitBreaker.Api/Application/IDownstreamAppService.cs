using Demo.CircuitBreaker.Api.Dtos;

namespace Demo.CircuitBreaker.Api.Application;

/// <summary>下游模拟应用服务契约。</summary>
public interface IDownstreamAppService
{
    Task<object> GetStatusAsync();

    Task<object> UpdateConfigAsync(DownstreamConfigDto input);
}
