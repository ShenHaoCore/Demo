namespace Demo.CircuitBreaker.Api.Application;

/// <summary>熔断器应用服务契约。</summary>
public interface ICircuitBreakerAppService
{
    Task<object> GetStatusAsync();

    Task<object> ResetAsync();
}
