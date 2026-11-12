using System.Collections.Concurrent;

namespace Demo.OrderTimeout.Api.Services;

/// <summary>进程内哈希时间轮调度。</summary>
public sealed class TimingWheelOrderTimeoutScheduler(HashedTimingWheel timingWheel) : IOrderTimeoutScheduler
{
    private readonly ConcurrentDictionary<Guid, Guid> _timeoutIds = new();

    public string ProviderName => "TimingWheel";

    public void Schedule(Guid orderId, TimeSpan delay, Action onTimeout)
    {
        var timeoutId = timingWheel.Schedule(delay, onTimeout);
        _timeoutIds[orderId] = timeoutId;
    }

    public void Cancel(Guid orderId)
    {
        if (_timeoutIds.TryRemove(orderId, out var timeoutId))
        {
            timingWheel.Cancel(timeoutId);
        }
    }
}
