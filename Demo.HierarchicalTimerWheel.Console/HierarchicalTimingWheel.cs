namespace Demo.HierarchicalTimerWheel.Core;

/// <summary>
/// 分层哈希时间轮（对照 hao_notes [[07-分层时间轮]]）。
/// 相对笔记初稿的可运行修正：
/// 1) 覆盖窗口随 currentTime 滑动；
/// 2) 先各层推进并收集，再统一经最细层重入（避免降级时细层时钟未跟上）；
/// 3) 时钟可注入，便于单测。
/// </summary>
public sealed class HierarchicalTimerWheel : IDisposable
{
    private readonly TimingWheel _root;
    private readonly TimingWheel[] _levels; // [0]=最细 … [^1]=最粗
    private readonly long _maxDelay;
    private readonly Func<long> _now;
    private readonly object _sync = new();
    private bool _disposed;

    public HierarchicalTimerWheel(HierarchicalTimerWheelOptions? options = null, Func<long>? clock = null)
    {
        options ??= new HierarchicalTimerWheelOptions();
        if (options.TickMs <= 0) throw new ArgumentOutOfRangeException(nameof(options.TickMs));
        if (options.WheelSize <= 1) throw new ArgumentOutOfRangeException(nameof(options.WheelSize));
        if (options.NumLevels <= 0) throw new ArgumentOutOfRangeException(nameof(options.NumLevels));

        _now = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var start = Align(_now(), options.TickMs);

        TimingWheel? overflow = null;
        var built = new TimingWheel[options.NumLevels];
        for (var i = options.NumLevels - 1; i >= 0; i--)
        {
            var layerTick = options.TickMs * Pow(options.WheelSize, i);
            overflow = new TimingWheel(layerTick, options.WheelSize, start, overflow);
            built[i] = overflow;
        }

        _levels = built;
        _root = built[0];
        _maxDelay = options.MaxDelayMs
            ?? checked(options.TickMs * Pow(options.WheelSize, options.NumLevels));
    }

    public long MaxDelayMs => _maxDelay;

    public static int CalculateLevels(long tickMs, int wheelSize, long maxDelayMs)
    {
        var levels = Math.Log((double)maxDelayMs / tickMs) / Math.Log(wheelSize);
        return (int)Math.Ceiling(levels);
    }

    public bool Schedule(long delayMs, Action callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(callback);
        if (delayMs <= 0 || delayMs > _maxDelay)
            return false;

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var now = _now();
            var task = new TimerTask(now + delayMs, callback);
            if (_root.AddTask(task, now))
                return true;

            callback();
            return true;
        }
    }

    /// <summary>
    /// 推进到 nowMs：各层先吐出到期桶内任务，再统一判定执行或经最细层重入。
    /// </summary>
    public int AdvanceClock(long nowMs)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var due = new List<Action>();

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var pending = new List<TimerTask>();
            // 粗→细推进时钟并收集；此时不重入，避免细层 currentTime 尚未跟上
            for (var i = _levels.Length - 1; i >= 0; i--)
                pending.AddRange(_levels[i].AdvanceAndFlush(nowMs));

            foreach (var task in pending)
            {
                if (task.Deadline <= nowMs)
                    due.Add(task.Callback);
                else
                    _root.AddTask(task, nowMs); // 各层时钟已对齐到 nowMs，滑动窗口正确
            }
        }

        foreach (var cb in due)
        {
            try { cb(); }
            catch { /* 教学：吞掉回调异常 */ }
        }

        return due.Count;
    }

    public void Dispose()
    {
        lock (_sync) _disposed = true;
    }

    private static long Align(long ms, long tickMs) => ms - (ms % tickMs);

    private static long Pow(int wheelSize, int exp)
    {
        long result = 1;
        for (var i = 0; i < exp; i++)
            result = checked(result * wheelSize);
        return result;
    }
}

public sealed class HierarchicalTimerWheelOptions
{
    public long TickMs { get; set; } = 1;
    public int WheelSize { get; set; } = 20;
    public int NumLevels { get; set; } = 5;
    public long? MaxDelayMs { get; set; }
}

internal sealed class TimerTask(long deadline, Action callback)
{
    public long Deadline { get; } = deadline;
    public Action Callback { get; } = callback;
    public bool IsCancelled { get; private set; }
    public void Cancel() => IsCancelled = true;
}

internal sealed class TimerTaskList
{
    private readonly LinkedList<TimerTask> _tasks = new();
    public long Expiration { get; set; } = long.MaxValue;

    public void AddTask(TimerTask task) => _tasks.AddLast(task);

    public List<TimerTask> Flush()
    {
        var list = new List<TimerTask>();
        var node = _tasks.First;
        while (node is not null)
        {
            var next = node.Next;
            if (!node.Value.IsCancelled)
                list.Add(node.Value);
            _tasks.Remove(node);
            node = next;
        }

        return list;
    }
}

internal sealed class TimingWheel
{
    private readonly int _wheelSize;
    private readonly long _tickMs;
    private readonly long _interval;
    private readonly TimerTaskList[] _buckets;
    private readonly TimingWheel? _overflow;
    private long _currentTime;

    public TimingWheel(long tickMs, int wheelSize, long startMs, TimingWheel? overflow)
    {
        _tickMs = tickMs;
        _wheelSize = wheelSize;
        _interval = tickMs * wheelSize;
        _overflow = overflow;
        _currentTime = startMs - (startMs % tickMs);
        _buckets = new TimerTaskList[wheelSize];
        for (var i = 0; i < wheelSize; i++)
            _buckets[i] = new TimerTaskList();
    }

    public bool AddTask(TimerTask task, long nowMs)
    {
        var deadline = task.Deadline;
        if (deadline <= nowMs)
            return false;

        // 滑动窗口：[currentTime, currentTime + interval)
        if (deadline < _currentTime + _interval)
        {
            var virtualId = deadline / _tickMs;
            var slot = (int)(virtualId % _wheelSize);
            _buckets[slot].AddTask(task);
            if (_buckets[slot].Expiration == long.MaxValue)
                _buckets[slot].Expiration = virtualId * _tickMs;
            return true;
        }

        return _overflow?.AddTask(task, nowMs) ?? false;
    }

    /// <summary>把时钟推到 nowMs，弹出沿途到期槽中的任务（不负责重入）。</summary>
    public List<TimerTask> AdvanceAndFlush(long nowMs)
    {
        var flushed = new List<TimerTask>();
        if (nowMs < _currentTime + _tickMs)
            return flushed;

        while (_currentTime + _tickMs <= nowMs)
        {
            _currentTime += _tickMs;
            var slot = (int)((_currentTime / _tickMs) % _wheelSize);
            var bucket = _buckets[slot];
            if (bucket.Expiration > _currentTime)
                continue;

            flushed.AddRange(bucket.Flush());
            bucket.Expiration = long.MaxValue;
        }

        return flushed;
    }
}
