using Demo.TimingWheel;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Demo.TimingWheel.Console：分层时间轮 ===\n");

using var wheel = new HierarchicalTimerWheel(new HierarchicalTimerWheelOptions
{
    TickMs = 1,
    WheelSize = 20,
    NumLevels = 3
});

Console.WriteLine($"最大覆盖: {wheel.MaxDelayMs} ms（TickMs×WheelSize^NumLevels）");
Console.WriteLine(
    $"CalculateLevels(1, 60, 24h) = {HierarchicalTimerWheel.CalculateLevels(1, 60, 24 * 3600 * 1000L)} 层\n");

var fired = new List<string>();
var sw = System.Diagnostics.Stopwatch.StartNew();

void Arm(string name, long delayMs)
{
    var ok = wheel.Schedule(delayMs, () =>
    {
        fired.Add(name);
        Console.WriteLine($"  [{sw.ElapsedMilliseconds,4} ms] {name} 到期（计划 {delayMs} ms）");
    });
    Console.WriteLine(ok ? $"已调度: {name} @ {delayMs} ms" : $"调度失败: {name}");
}

Arm("短延迟（最细层）", 80);
Arm("跨层降级", 150);
Arm("稍后", 220);

Console.WriteLine("\n推进中…\n");
var until = DateTime.UtcNow.AddMilliseconds(600);
while (DateTime.UtcNow < until && fired.Count < 3)
{
    wheel.AdvanceClock(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    Thread.Sleep(5);
}

Console.WriteLine($"\n完成：{fired.Count}/3");
Environment.ExitCode = fired.Count == 3 ? 0 : 1;
Console.WriteLine(fired.Count == 3 ? "分层时间轮运行正常。" : "有任务未触发。");
