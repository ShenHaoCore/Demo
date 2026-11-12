using System.Collections.Concurrent;

namespace Demo.OrderTimeout.Api.Services;

/// <summary>
/// 哈希时间轮（教学版，思路对齐 Netty HashedWheelTimer）：
/// 单层轮 + rounds。默认 tick=1s、wheelSize=60（一圈 60s）；
/// 30 分钟 = 1800 tick → 约 30 圈后触发关单。
/// </summary>
public sealed class HashedTimingWheel : IDisposable
{
    private readonly object _sync = new();
    private readonly LinkedList<WheelTimeout>[] _buckets;
    private readonly ConcurrentDictionary<Guid, WheelTimeout> _timeouts = new();
    private readonly TimeSpan _tickDuration;
    private readonly int _wheelSize;
    private readonly ILogger<HashedTimingWheel> _logger;

    private long _ticks;
    private bool _disposed;

    public HashedTimingWheel(
        ILogger<HashedTimingWheel> logger,
        TimeSpan? tickDuration = null,
        int wheelSize = 60)
    {
        if (wheelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wheelSize));
        }

        _logger = logger;
        _tickDuration = tickDuration ?? TimeSpan.FromSeconds(1);
        _wheelSize = wheelSize;
        _buckets = new LinkedList<WheelTimeout>[wheelSize];
        for (var i = 0; i < wheelSize; i++)
        {
            _buckets[i] = new LinkedList<WheelTimeout>();
        }
    }

    public TimeSpan TickDuration => _tickDuration;

    public int WheelSize => _wheelSize;

    /// <summary>调度延迟任务；返回 timeoutId，可用于提前取消。</summary>
    public Guid Schedule(TimeSpan delay, Action callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(callback);

        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        var delayTicks = Math.Max(
            1L,
            (long)Math.Ceiling(delay.TotalMilliseconds / _tickDuration.TotalMilliseconds));

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // 下一格起算：AdvanceClock 先 ++ticks 再处理 buckets[ticks % size]
            var deadline = _ticks + delayTicks;
            var stopIndex = (int)(deadline % _wheelSize);
            var remainingRounds = (int)((delayTicks - 1) / _wheelSize);

            var timeout = new WheelTimeout(Guid.NewGuid(), callback, remainingRounds);
            _buckets[stopIndex].AddLast(timeout);
            _timeouts[timeout.Id] = timeout;
            return timeout.Id;
        }
    }

    public bool Cancel(Guid timeoutId)
    {
        if (!_timeouts.TryRemove(timeoutId, out var timeout))
        {
            return false;
        }

        timeout.Cancel();
        return true;
    }

    /// <summary>推进一格；到期任务在调用线程执行。</summary>
    public void AdvanceClock()
    {
        List<Action>? due = null;

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _ticks++;
            var bucket = _buckets[(int)(_ticks % _wheelSize)];
            var node = bucket.First;
            while (node is not null)
            {
                var next = node.Next;
                var timeout = node.Value;

                if (timeout.IsCancelled)
                {
                    bucket.Remove(node);
                    _timeouts.TryRemove(timeout.Id, out _);
                }
                else if (timeout.RemainingRounds > 0)
                {
                    timeout.RemainingRounds--;
                }
                else
                {
                    bucket.Remove(node);
                    _timeouts.TryRemove(timeout.Id, out _);
                    due ??= [];
                    due.Add(timeout.Callback);
                }

                node = next;
            }
        }

        if (due is null)
        {
            return;
        }

        foreach (var action in due)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "时间轮任务执行失败");
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var bucket in _buckets)
            {
                bucket.Clear();
            }

            _timeouts.Clear();
        }
    }

    private sealed class WheelTimeout(Guid id, Action callback, int remainingRounds)
    {
        public Guid Id { get; } = id;

        public Action Callback { get; } = callback;

        public int RemainingRounds { get; set; } = remainingRounds;

        public bool IsCancelled { get; private set; }

        public void Cancel() => IsCancelled = true;
    }
}
