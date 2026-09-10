namespace Demo.CircuitBreaker.Api.Application;

/// <summary>模拟下游开关选项。</summary>
public sealed class DownstreamOptions
{
    public bool ForceFail { get; set; } = true;
}

/// <summary>下游故障异常。</summary>
public sealed class DownstreamException(string message) : Exception(message);

/// <summary>熔断器打开时拒绝调用。</summary>
public sealed class CircuitOpenException(string message) : Exception(message);

/// <summary>熔断器状态。</summary>
public enum BreakerState
{
    Closed,
    Open,
    HalfOpen
}

/// <summary>简易熔断器（Closed / Open / HalfOpen）。</summary>
public sealed class SimpleCircuitBreaker
{
    private readonly object _sync = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly int _halfOpenMaxProbes;
    private BreakerState _state = BreakerState.Closed;
    private int _consecutiveFailures;
    private DateTimeOffset _openedAt;
    private int _halfOpenSuccesses;
    private int _halfOpenInFlight;
    private int _totalCalls;
    private int _rejectedCalls;

    public SimpleCircuitBreaker(int failureThreshold = 3, int openSeconds = 10, int halfOpenMaxProbes = 1)
    {
        _failureThreshold = failureThreshold;
        _openDuration = TimeSpan.FromSeconds(openSeconds);
        _halfOpenMaxProbes = Math.Max(1, halfOpenMaxProbes);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        lock (_sync)
        {
            _totalCalls++;
            TransitionIfNeeded_NoLock();

            if (_state == BreakerState.Open)
            {
                _rejectedCalls++;
                throw new CircuitOpenException($"熔断器处于 Open 状态，请约 {_openDuration.TotalSeconds} 秒后再试");
            }

            // Half-Open：限制并发探测数（经典实现常为 1）
            if (_state == BreakerState.HalfOpen)
            {
                if (_halfOpenInFlight >= _halfOpenMaxProbes)
                {
                    _rejectedCalls++;
                    throw new CircuitOpenException(
                        $"熔断器处于 HalfOpen，探测名额已满（最多 {_halfOpenMaxProbes} 个并发试探）");
                }

                _halfOpenInFlight++;
            }
        }

        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch
        {
            OnFailure();
            throw;
        }
    }

    public object GetStatus()
    {
        lock (_sync)
        {
            TransitionIfNeeded_NoLock();
            return new
            {
                state = _state.ToString(),
                consecutiveFailures = _consecutiveFailures,
                failureThreshold = _failureThreshold,
                openDurationSeconds = _openDuration.TotalSeconds,
                openedAt = _state == BreakerState.Closed ? (DateTimeOffset?)null : _openedAt,
                halfOpenMaxProbes = _halfOpenMaxProbes,
                halfOpenInFlight = _halfOpenInFlight,
                halfOpenSuccesses = _halfOpenSuccesses,
                totalCalls = _totalCalls,
                rejectedCalls = _rejectedCalls,
                message = _state switch
                {
                    BreakerState.Closed => "闭合：正常放行",
                    BreakerState.Open => "打开：拒绝调用",
                    BreakerState.HalfOpen => $"半开：最多 {_halfOpenMaxProbes} 个并发试探",
                    _ => "未知状态"
                }
            };
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _state = BreakerState.Closed;
            _consecutiveFailures = 0;
            _halfOpenSuccesses = 0;
            _halfOpenInFlight = 0;
            _openedAt = default;
        }
    }

    private void OnSuccess()
    {
        lock (_sync)
        {
            _consecutiveFailures = 0;
            if (_state == BreakerState.HalfOpen)
            {
                _halfOpenInFlight = Math.Max(0, _halfOpenInFlight - 1);
                _halfOpenSuccesses++;
                if (_halfOpenSuccesses >= _halfOpenMaxProbes)
                {
                    _state = BreakerState.Closed;
                    _halfOpenSuccesses = 0;
                    _halfOpenInFlight = 0;
                }
            }
        }
    }

    private void OnFailure()
    {
        lock (_sync)
        {
            if (_state == BreakerState.HalfOpen)
            {
                _halfOpenInFlight = Math.Max(0, _halfOpenInFlight - 1);
            }

            _consecutiveFailures++;
            if (_state == BreakerState.HalfOpen || _consecutiveFailures >= _failureThreshold)
            {
                _state = BreakerState.Open;
                _openedAt = DateTimeOffset.UtcNow;
                _halfOpenSuccesses = 0;
                _halfOpenInFlight = 0;
            }
        }
    }

    private void TransitionIfNeeded_NoLock()
    {
        if (_state == BreakerState.Open && DateTimeOffset.UtcNow - _openedAt >= _openDuration)
        {
            _state = BreakerState.HalfOpen;
            _halfOpenSuccesses = 0;
            _halfOpenInFlight = 0;
        }
    }
}
