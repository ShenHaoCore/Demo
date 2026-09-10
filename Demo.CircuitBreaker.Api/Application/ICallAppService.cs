namespace Demo.CircuitBreaker.Api.Application;

/// <summary>经熔断器调用下游的应用服务契约。</summary>
public interface ICallAppService
{
    Task<object> CallDownstreamAsync();
}
