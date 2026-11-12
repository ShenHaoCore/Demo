using System.Diagnostics;
using Demo.TimingWheel;

// 标准演示：调度 → 推进 → 触发；含跨层与 Cancel

Console.OutputEncoding = System.Text.Encoding.UTF8;

using var wheel = new HierarchicalTimerWheel(new() { TickMs = 1, WheelSize = 20, NumLevels = 3 });
var sw = Stopwatch.StartNew();
var fired = 0;

Console.WriteLine($"HierarchicalTimerWheel  MaxDelay={wheel.MaxDelayMs}ms");
Console.WriteLine($"levels for 24h@1ms×60 = {HierarchicalTimerWheel.CalculateLevels(1, 60, 24 * 3600 * 1000L)}");
Console.WriteLine();

Arm("fine", 80);   // 落细层
Arm("cross", 150); // >20ms，先上粗层再降级
Arm("late", 220);

var cancelled = wheel.Schedule(500, () => Console.WriteLine("  should not fire"));
cancelled!.Cancel();
Console.WriteLine("schedule cancel@500ms  (will not fire)");
Console.WriteLine();

var until = sw.ElapsedMilliseconds + 400;
while (sw.ElapsedMilliseconds < until && fired < 3)
{
    wheel.AdvanceClock(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    Thread.Sleep(5);
}

Console.WriteLine();
Console.WriteLine($"{fired}/3 fired");
Environment.ExitCode = fired == 3 ? 0 : 1;

void Arm(string name, long delayMs)
{
    wheel.Schedule(delayMs, () =>
    {
        Interlocked.Increment(ref fired);
        Console.WriteLine($"  [{sw.ElapsedMilliseconds,3}ms] {name}");
    });
    Console.WriteLine($"schedule {name}@{delayMs}ms");
}
