namespace Demo.CircuitBreaker.Api.Application;

/// <summary>熔断器应用服务。</summary>
public sealed class CircuitBreakerAppService(SimpleCircuitBreaker breaker) : ICircuitBreakerAppService
{
    public Task<object> GetStatusAsync() => Task.FromResult(breaker.GetStatus());

    public Task<object> ResetAsync()
    {
        breaker.Reset();
        return Task.FromResult(breaker.GetStatus());
    }
}
