namespace Demo.TimingWheel;

/// <summary>
/// 分层哈希时间轮。窗口随 currentTime 滑动；推进粗→细收集后再经最细层重入。
/// MaxDelay = TickMs × WheelSize^NumLevels
/// </summary>
public sealed class HierarchicalTimerWheel : IDisposable
{
    private readonly TimingWheel _root;     // 最细层
    private readonly TimingWheel[] _levels; // [0]=最细 … [^1]=最粗
    private readonly long _maxDelay;
    private readonly Func<long> _now;
    private readonly object _sync = new();
    private bool _disposed;

    public HierarchicalTimerWheel(HierarchicalTimerWheelOptions? options = null, Func<long>? clock = null)
    {
        options ??= new HierarchicalTimerWheelOptions();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.TickMs);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.WheelSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.NumLevels);

        _now = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var now = _now();
        var start = now - now % options.TickMs;

        TimingWheel? overflow = null;
        _levels = new TimingWheel[options.NumLevels];
        for (var i = options.NumLevels - 1; i >= 0; i--) // 粗→细倒建
        {
            var tick = options.TickMs * Pow(options.WheelSize, i);
            overflow = new TimingWheel(tick, options.WheelSize, start, overflow);
            _levels[i] = overflow;
        }

        _root = _levels[0];
        _maxDelay = options.MaxDelayMs
            ?? checked(options.TickMs * Pow(options.WheelSize, options.NumLevels));
    }

    public long MaxDelayMs => _maxDelay;

    public static int CalculateLevels(long tickMs, int wheelSize, long maxDelayMs)
        => (int)Math.Ceiling(Math.Log((double)maxDelayMs / tickMs) / Math.Log(wheelSize));

    /// <summary>挂延迟任务；句柄可惰性取消。非法 delay 返回 null。</summary>
    public TimerHandle? Schedule(long delayMs, Action callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(callback);
        if (delayMs <= 0 || delayMs > _maxDelay)
            return null;

        TimerTask task;
        var invokeNow = false;
        lock (_sync)
        {
            var now = _now();
            task = new TimerTask(now + delayMs, callback);
            if (!_root.AddTask(task, now))
                invokeNow = true;
        }

        if (invokeNow)
            InvokeSafe(task);
        return new TimerHandle(task);
    }

    public int AdvanceClock(long nowMs)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        List<TimerTask> due;

        lock (_sync)
        {
            var pending = new List<TimerTask>();
            for (var i = _levels.Length - 1; i >= 0; i--)
                _levels[i].AdvanceAndFlush(nowMs, pending);

            due = new List<TimerTask>(pending.Count);
            foreach (var task in pending)
            {
                if (task.Deadline <= nowMs)
                    due.Add(task);
                else
                    _root.AddTask(task, nowMs);
            }
        }

        var ran = 0;
        foreach (var task in due)
        {
            if (task.IsCancelled) continue;
            InvokeSafe(task);
            ran++;
        }
        return ran;
    }

    public void Dispose()
    {
        lock (_sync) _disposed = true;
    }

    private static void InvokeSafe(TimerTask task)
    {
        if (task.IsCancelled) return;
        try { task.Callback(); }
        catch { /* 演示：勿打断推进 */ }
    }

    private static long Pow(int baseValue, int exp)
    {
        long n = 1;
        for (var i = 0; i < exp; i++)
            n = checked(n * baseValue);
        return n;
    }
}

public sealed class HierarchicalTimerWheelOptions
{
    public long TickMs { get; set; } = 1;
    public int WheelSize { get; set; } = 20;
    public int NumLevels { get; set; } = 5;
    public long? MaxDelayMs { get; set; }
}

/// <summary>Schedule 返回值；Cancel 惰性生效。</summary>
public sealed class TimerHandle
{
    private readonly TimerTask _task;
    internal TimerHandle(TimerTask task) => _task = task;
    public bool IsCancelled => _task.IsCancelled;
    public void Cancel() => _task.Cancel();
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

    public void FlushTo(List<TimerTask> destination)
    {
        for (var node = _tasks.First; node is not null;)
        {
            var next = node.Next;
            if (!node.Value.IsCancelled)
                destination.Add(node.Value);
            _tasks.Remove(node);
            node = next;
        }
    }
}

/// <summary>单层轮，覆盖 [currentTime, currentTime + tick×size)。</summary>
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
        _currentTime = startMs - startMs % tickMs;
        _buckets = new TimerTaskList[wheelSize];
        for (var i = 0; i < wheelSize; i++)
            _buckets[i] = new TimerTaskList();
    }

    public bool AddTask(TimerTask task, long nowMs)
    {
        if (task.Deadline <= nowMs)
            return false;

        if (task.Deadline < _currentTime + _interval)
        {
            var id = task.Deadline / _tickMs;
            var slot = (int)(id % _wheelSize);
            _buckets[slot].AddTask(task);
            if (_buckets[slot].Expiration == long.MaxValue)
                _buckets[slot].Expiration = id * _tickMs;
            return true;
        }

        return _overflow?.AddTask(task, nowMs) ?? false;
    }

    public void AdvanceAndFlush(long nowMs, List<TimerTask> destination)
    {
        while (_currentTime + _tickMs <= nowMs)
        {
            _currentTime += _tickMs;
            var bucket = _buckets[(int)((_currentTime / _tickMs) % _wheelSize)];
            if (bucket.Expiration > _currentTime)
                continue;

            bucket.FlushTo(destination);
            bucket.Expiration = long.MaxValue;
        }
    }
}
