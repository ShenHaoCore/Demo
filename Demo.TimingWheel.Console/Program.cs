using Demo.TimingWheel;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Demo.TimingWheel.Console ===\n");

using var wheel = new HierarchicalTimerWheel(new()
{
    TickMs = 1,
    WheelSize = 20,
    NumLevels = 3 // 覆盖 1×20³=8000ms
});

Console.WriteLine($"MaxDelay={wheel.MaxDelayMs}ms  Levels(1,60,24h)={HierarchicalTimerWheel.CalculateLevels(1, 60, 24 * 3600 * 1000L)}\n");

var fired = 0;
var sw = System.Diagnostics.Stopwatch.StartNew();

void Arm(string name, long delayMs)
{
    wheel.Schedule(delayMs, () =>
    {
        Interlocked.Increment(ref fired);
        Console.WriteLine($"  [{sw.ElapsedMilliseconds,4}ms] {name}");
    });
    Console.WriteLine($"调度 {name} @{delayMs}ms");
}

Arm("细层", 80);
Arm("跨层", 150); // > WheelSize×TickMs，先上粗层再降级
Arm("稍后", 220);

Console.WriteLine("\n推进…\n");
var until = DateTime.UtcNow.AddMilliseconds(600);
while (DateTime.UtcNow < until && fired < 3)
{
    wheel.AdvanceClock(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    Thread.Sleep(5);
}

Console.WriteLine($"\n{fired}/3 {(fired == 3 ? "OK" : "FAIL")}");
Environment.ExitCode = fired == 3 ? 0 : 1;
