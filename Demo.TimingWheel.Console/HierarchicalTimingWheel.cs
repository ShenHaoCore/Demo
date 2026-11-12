namespace Demo.TimingWheel;

/// <summary>
/// 分层哈希时间轮。要点：
/// 1) 窗口随 currentTime 滑动（勿写死构造时 EndTime）；
/// 2) 推进时粗→细先收集，再经最细层重入；
/// 3) clock 可注入，便于单测。
/// MaxDelay = TickMs × WheelSize^NumLevels
/// </summary>
public sealed class HierarchicalTimerWheel : IDisposable
{
    private readonly TimingWheel _root;       // 最细层，Schedule 入口
    private readonly TimingWheel[] _levels;   // [0]=最细 … [^1]=最粗
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

        // 从粗到细倒建：overflow 指向上层粗轮
        TimingWheel? overflow = null;
        _levels = new TimingWheel[options.NumLevels];
        for (var i = options.NumLevels - 1; i >= 0; i--)
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

    /// <summary>N = ceil(log(maxDelay/tick) / log(wheelSize))</summary>
    public static int CalculateLevels(long tickMs, int wheelSize, long maxDelayMs)
        => (int)Math.Ceiling(Math.Log((double)maxDelayMs / tickMs) / Math.Log(wheelSize));

    public bool Schedule(long delayMs, Action callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(callback);
        if (delayMs <= 0 || delayMs > _maxDelay)
            return false;

        lock (_sync)
        {
            var now = _now();
            if (_root.AddTask(new TimerTask(now + delayMs, callback), now))
                return true;

            callback(); // 入轮瞬间已到期
            return true;
        }
    }

    /// <summary>推进到 nowMs：先各层吐任务，再执行或经 _root 重入。</summary>
    public int AdvanceClock(long nowMs)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        List<Action> due;

        lock (_sync)
        {
            var pending = new List<TimerTask>();
            for (var i = _levels.Length - 1; i >= 0; i--)
                pending.AddRange(_levels[i].AdvanceAndFlush(nowMs));

            due = [];
            foreach (var task in pending)
            {
                if (task.Deadline <= nowMs)
                    due.Add(task.Callback);
                else
                    _root.AddTask(task, nowMs); // 时钟已对齐，可正确落细层
            }
        }

        foreach (var cb in due)
        {
            try { cb(); }
            catch { /* 演示：勿打断推进 */ }
        }

        return due.Count;
    }

    public void Dispose()
    {
        lock (_sync) _disposed = true;
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
    public long TickMs { get; set; } = 1;       // 最细精度
    public int WheelSize { get; set; } = 20;    // 每层槽数
    public int NumLevels { get; set; } = 5;     // 层数
    public long? MaxDelayMs { get; set; }       // 不设则按公式算
}

internal sealed class TimerTask(long deadline, Action callback)
{
    public long Deadline { get; } = deadline;   // 绝对 Unix ms
    public Action Callback { get; } = callback;
    public bool IsCancelled { get; private set; }
    public void Cancel() => IsCancelled = true; // 惰性取消，Flush 时跳过
}

internal sealed class TimerTaskList
{
    private readonly LinkedList<TimerTask> _tasks = new();
    public long Expiration { get; set; } = long.MaxValue; // 整桶到期时刻；MaxValue=空

    public void AddTask(TimerTask task) => _tasks.AddLast(task);

    public List<TimerTask> Flush()
    {
        var list = new List<TimerTask>();
        for (var node = _tasks.First; node is not null;)
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

/// <summary>单层轮。覆盖 [currentTime, currentTime + tick×size)。</summary>
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

        return _overflow?.AddTask(task, nowMs) ?? false; // 升层
    }

    /// <summary>只推进并吐出任务，不负责重入。</summary>
    public List<TimerTask> AdvanceAndFlush(long nowMs)
    {
        var flushed = new List<TimerTask>();
        while (_currentTime + _tickMs <= nowMs)
        {
            _currentTime += _tickMs;
            var bucket = _buckets[(int)((_currentTime / _tickMs) % _wheelSize)];
            if (bucket.Expiration > _currentTime)
                continue;

            flushed.AddRange(bucket.Flush());
            bucket.Expiration = long.MaxValue;
        }

        return flushed;
    }
}
