using Demo.Retry.Api.Dtos;

namespace Demo.Retry.Api.Services;

/// <summary>不稳定下游模拟（基础设施）。</summary>
public sealed class UnstableDownstream
{
    private int _callCount;

    public double FailureRate { get; set; } = 0.7;

    public UnstableResultDto Invoke()
    {
        var attempt = Interlocked.Increment(ref _callCount);
        var random = Random.Shared.NextDouble();
        if (random < FailureRate)
        {
            return new UnstableResultDto(false, attempt, FailureRate, "下游服务暂时不可用", DateTimeOffset.UtcNow);
        }

        return new UnstableResultDto(true, attempt, FailureRate, "下游服务调用成功", DateTimeOffset.UtcNow);
    }
}
