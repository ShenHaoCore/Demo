using Demo.TimingWheel;

namespace Demo.TimingWheel.Console;

public class TimingWheelTests
{
    [Fact]
    public void CalculateLevels_由精度与最大延迟反推层数()
    {
        // 60^4=1.296e7 < 864e5(24h) ≤ 60^5 → N=5
        var levels = HierarchicalTimerWheel.CalculateLevels(1, 60, 24 * 3600 * 1000L);
        Assert.Equal(5, levels);
    }

    [Fact]
    public void Schedule_短延迟到期后回调执行()
    {
        long now = 1_000_000;
        using var wheel = new HierarchicalTimerWheel(
            new HierarchicalTimerWheelOptions { TickMs = 1, WheelSize = 20, NumLevels = 3 },
            clock: () => now);

        var fired = 0;
        Assert.True(wheel.Schedule(50, () => fired++));

        now = 1_000_040;
        Assert.Equal(0, wheel.AdvanceClock(now));
        Assert.Equal(0, fired);

        now = 1_000_060;
        Assert.True(wheel.AdvanceClock(now) >= 1);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Schedule_超出最细层范围仍能经上层降级到期()
    {
        long now = 2_000_000;
        using var wheel = new HierarchicalTimerWheel(
            new HierarchicalTimerWheelOptions { TickMs = 1, WheelSize = 20, NumLevels = 3 },
            clock: () => now);

        var fired = false;
        Assert.True(wheel.Schedule(200, () => fired = true));

        now = 2_000_100;
        wheel.AdvanceClock(now);
        Assert.False(fired);

        now = 2_000_220;
        wheel.AdvanceClock(now);
        Assert.True(fired);
    }

    [Fact]
    public void Schedule_超过MaxDelay返回false()
    {
        using var wheel = new HierarchicalTimerWheel(
            new HierarchicalTimerWheelOptions
            {
                TickMs = 1,
                WheelSize = 20,
                NumLevels = 2,
                MaxDelayMs = 100
            },
            clock: () => 0);

        Assert.False(wheel.Schedule(101, () => { }));
        Assert.True(wheel.Schedule(100, () => { }));
    }

    [Fact]
    public void Schedule_多个任务按时间先后触发()
    {
        long now = 3_000_000;
        using var wheel = new HierarchicalTimerWheel(
            new HierarchicalTimerWheelOptions { TickMs = 1, WheelSize = 20, NumLevels = 4 },
            clock: () => now);

        var order = new List<int>();
        Assert.True(wheel.Schedule(30, () => order.Add(1)));
        Assert.True(wheel.Schedule(10, () => order.Add(2)));
        Assert.True(wheel.Schedule(20, () => order.Add(3)));

        now = 3_000_015;
        wheel.AdvanceClock(now);
        Assert.Equal(new[] { 2 }, order);

        now = 3_000_025;
        wheel.AdvanceClock(now);
        Assert.Equal(new[] { 2, 3 }, order);

        now = 3_000_040;
        wheel.AdvanceClock(now);
        Assert.Equal(new[] { 2, 3, 1 }, order);
    }

    [Fact]
    public void 真实时钟_短延迟可跑通()
    {
        using var wheel = new HierarchicalTimerWheel(
            new HierarchicalTimerWheelOptions { TickMs = 1, WheelSize = 20, NumLevels = 3 });

        using var fired = new ManualResetEventSlim(false);
        Assert.True(wheel.Schedule(80, fired.Set));

        var deadline = DateTime.UtcNow.AddMilliseconds(500);
        while (DateTime.UtcNow < deadline && !fired.IsSet)
        {
            wheel.AdvanceClock(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Thread.Sleep(5);
        }

        Assert.True(fired.IsSet, "80ms 任务应在 500ms 内触发");
    }
}
