using Demo.TimingWheel;

namespace Demo.TimingWheel.Console;

public class TimingWheelTests
{
    [Fact]
    public void CalculateLevels_24h需要5层()
        // 60^4≈3.6h < 24h ≤ 60^5
        => Assert.Equal(5, HierarchicalTimerWheel.CalculateLevels(1, 60, 24 * 3600 * 1000L));

    [Fact]
    public void 短延迟到期执行()
    {
        long now = 1_000_000;
        using var w = new HierarchicalTimerWheel(
            new() { TickMs = 1, WheelSize = 20, NumLevels = 3 }, () => now);
        var n = 0;
        Assert.True(w.Schedule(50, () => n++));

        now += 40;
        Assert.Equal(0, w.AdvanceClock(now));

        now += 20;
        Assert.True(w.AdvanceClock(now) >= 1);
        Assert.Equal(1, n);
    }

    [Fact]
    public void 跨层降级后到期()
    {
        long now = 2_000_000;
        using var w = new HierarchicalTimerWheel(
            new() { TickMs = 1, WheelSize = 20, NumLevels = 3 }, () => now);
        var ok = false;
        Assert.True(w.Schedule(200, () => ok = true)); // 细层只盖 20ms

        now += 100;
        w.AdvanceClock(now);
        Assert.False(ok);

        now += 120;
        w.AdvanceClock(now);
        Assert.True(ok);
    }

    [Fact]
    public void 超MaxDelay拒绝()
    {
        using var w = new HierarchicalTimerWheel(new()
        {
            TickMs = 1,
            WheelSize = 20,
            NumLevels = 2,
            MaxDelayMs = 100
        }, () => 0);

        Assert.False(w.Schedule(101, () => { }));
        Assert.True(w.Schedule(100, () => { }));
    }

    [Fact]
    public void 多任务按时间顺序()
    {
        long now = 3_000_000;
        using var w = new HierarchicalTimerWheel(
            new() { TickMs = 1, WheelSize = 20, NumLevels = 4 }, () => now);
        var order = new List<int>();
        w.Schedule(30, () => order.Add(1));
        w.Schedule(10, () => order.Add(2));
        w.Schedule(20, () => order.Add(3));

        now += 15; w.AdvanceClock(now);
        Assert.Equal([2], order);
        now += 10; w.AdvanceClock(now);
        Assert.Equal([2, 3], order);
        now += 15; w.AdvanceClock(now);
        Assert.Equal([2, 3, 1], order);
    }

    [Fact]
    public void 真实时钟冒烟()
    {
        using var w = new HierarchicalTimerWheel(new() { TickMs = 1, WheelSize = 20, NumLevels = 3 });
        using var fired = new ManualResetEventSlim(false);
        Assert.True(w.Schedule(80, fired.Set));

        var until = DateTime.UtcNow.AddMilliseconds(500);
        while (DateTime.UtcNow < until && !fired.IsSet)
        {
            w.AdvanceClock(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Thread.Sleep(5);
        }

        Assert.True(fired.IsSet);
    }
}
