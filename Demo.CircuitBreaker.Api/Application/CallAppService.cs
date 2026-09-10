namespace Demo.CircuitBreaker.Api.Application;

/// <summary>经熔断器调用下游的应用服务。</summary>
public sealed class CallAppService(
    SimpleCircuitBreaker breaker,
    DownstreamOptions options) : ICallAppService
{
    public async Task<object> CallDownstreamAsync()
    {
        var result = await breaker.ExecuteAsync(async () =>
        {
            if (options.ForceFail)
            {
                throw new DownstreamException("下游服务故障");
            }

            await Task.Delay(10);
            return new
            {
                success = true,
                message = "经熔断器调用下游成功",
                timestamp = DateTimeOffset.UtcNow
            };
        });

        return new
        {
            result,
            breaker = breaker.GetStatus()
        };
    }
}
