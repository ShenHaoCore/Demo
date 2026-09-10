using Demo.Retry.Api.Dtos;
using Demo.Retry.Api.Services;
using Polly;

namespace Demo.Retry.Api.Application;

/// <summary>重试演示应用服务。</summary>
public sealed class RetryDemoAppService(
    UnstableDownstream downstream,
    ResiliencePipeline<UnstableResultDto> pipeline,
    ILogger<RetryDemoAppService> logger) : IRetryDemoAppService
{
    public Task<UnstableInvokeResult> InvokeUnstableAsync(double? rate)
    {
        if (rate is >= 0 and <= 1)
        {
            downstream.FailureRate = rate.Value;
            logger.LogInformation("已更新不稳定接口失败率：{FailureRate}", downstream.FailureRate);
        }

        var result = downstream.Invoke();
        return Task.FromResult(new UnstableInvokeResult
        {
            IsSuccess = result.Success,
            Body = result
        });
    }

    public Task<object> UpdateConfigAsync(UnstableConfigDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.FailureRate is < 0 or > 1)
        {
            throw new ArgumentException("失败率必须在 0 到 1 之间", nameof(input.FailureRate));
        }

        downstream.FailureRate = input.FailureRate;
        logger.LogInformation("通过配置接口设置失败率：{FailureRate}", downstream.FailureRate);
        return Task.FromResult<object>(new { failureRate = downstream.FailureRate, message = "失败率已更新" });
    }

    public async Task<object> ProxyWithRetryAsync()
    {
        var attempts = 0;
        UnstableResultDto? lastResult = null;
        string? error = null;
        var succeeded = false;

        try
        {
            lastResult = await pipeline.ExecuteAsync(_ =>
            {
                attempts++;
                return ValueTask.FromResult(downstream.Invoke());
            });
            succeeded = lastResult.Success;
            if (!succeeded)
            {
                error = "达到最大重试次数后仍失败";
            }
        }
        catch (Exception ex)
        {
            error = $"重试过程异常：{ex.Message}";
            logger.LogError(ex, "代理调用不稳定下游失败");
        }

        return new
        {
            success = succeeded,
            attempts,
            failureRate = downstream.FailureRate,
            result = lastResult,
            error,
            message = succeeded ? "代理重试后调用成功" : "代理重试后调用失败"
        };
    }
}
