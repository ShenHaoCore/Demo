using Demo.TimingWheel;

namespace Demo.TimingWheel.Console;

public class TimingWheelTests
{
    [Fact]
    public void CalculateLevels_24h_Needs5Levels()
        => Assert.Equal(5, HierarchicalTimerWheel.CalculateLevels(1, 60, 24 * 3600 * 1000L));

    [Fact]
    public void ShortDelay_FiresWhenDue()
    {
        long now = 1_000_000;
        using var w = new HierarchicalTimerWheel(new() { TickMs = 1, WheelSize = 20, NumLevels = 3 }, () => now);
        var n = 0;
        Assert.NotNull(w.Schedule(50, () => n++));

        now += 40;
        Assert.Equal(0, w.AdvanceClock(now));
        now += 20;
        Assert.True(w.AdvanceClock(now) >= 1);
        Assert.Equal(1, n);
    }

    [Fact]
    public void CrossLevel_CascadesThenFires()
    {
        long now = 2_000_000;
        using var w = new HierarchicalTimerWheel(new() { TickMs = 1, WheelSize = 20, NumLevels = 3 }, () => now);
        var ok = false;
        Assert.NotNull(w.Schedule(200, () => ok = true)); // 细层只盖 20ms

        now += 100; w.AdvanceClock(now);
        Assert.False(ok);
        now += 120; w.AdvanceClock(now);
        Assert.True(ok);
    }

    [Fact]
    public void BeyondMaxDelay_Rejected()
    {
        using var w = new HierarchicalTimerWheel(
            new() { TickMs = 1, WheelSize = 20, NumLevels = 2, MaxDelayMs = 100 }, () => 0);
        Assert.Null(w.Schedule(101, () => { }));
        Assert.NotNull(w.Schedule(100, () => { }));
    }

    [Fact]
    public void MultipleTasks_FireInTimeOrder()
    {
        long now = 3_000_000;
        using var w = new HierarchicalTimerWheel(new() { TickMs = 1, WheelSize = 20, NumLevels = 4 }, () => now);
        var order = new List<int>();
        w.Schedule(30, () => order.Add(1));
        w.Schedule(10, () => order.Add(2));
        w.Schedule(20, () => order.Add(3));

        now += 15; w.AdvanceClock(now); Assert.Equal([2], order);
        now += 10; w.AdvanceClock(now); Assert.Equal([2, 3], order);
        now += 15; w.AdvanceClock(now); Assert.Equal([2, 3, 1], order);
    }

    [Fact]
    public void CancelBeforeDue_DoesNotFire()
    {
        long now = 4_000_000;
        using var w = new HierarchicalTimerWheel(new() { TickMs = 1, WheelSize = 20, NumLevels = 3 }, () => now);
        var closed = false;
        var handle = w.Schedule(100, () => closed = true);
        Assert.NotNull(handle);

        now += 40; w.AdvanceClock(now);
        handle.Cancel();
        now += 80;
        Assert.Equal(0, w.AdvanceClock(now));
        Assert.False(closed);
    }
}
