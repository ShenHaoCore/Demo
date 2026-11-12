using Demo.HierarchicalTimerWheel.Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Demo.HierarchicalTimerWheel.Console：分层时间轮演示 ===\n");

using var wheel = new HierarchicalTimerWheel(new HierarchicalTimerWheelOptions
{
    TickMs = 1,
    WheelSize = 20,
    NumLevels = 3
});

Console.WriteLine($"最大覆盖延迟: {wheel.MaxDelayMs} ms");
Console.WriteLine(
    $"CalculateLevels(1,60,24h) = {HierarchicalTimerWheel.CalculateLevels(1, 60, 24 * 3600 * 1000L)} 层\n");

var fired = new List<(string Name, long ElapsedMs)>();
var sw = System.Diagnostics.Stopwatch.StartNew();

void Arm(string name, long delayMs)
{
    var ok = wheel.Schedule(delayMs, () =>
    {
        fired.Add((name, sw.ElapsedMilliseconds));
        Console.WriteLine($"  [{sw.ElapsedMilliseconds,4} ms] {name} 到期（计划 {delayMs} ms）");
    });
    Console.WriteLine(ok
        ? $"已调度: {name} @ {delayMs} ms"
        : $"调度失败: {name} @ {delayMs} ms");
}

Arm("短延迟（最细层内）", 80);
Arm("跨层降级（>20ms）", 150);
Arm("稍后一点", 220);

Console.WriteLine("\n推进时钟中…\n");
var deadline = DateTime.UtcNow.AddMilliseconds(600);
while (DateTime.UtcNow < deadline && fired.Count < 3)
{
    wheel.AdvanceClock(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    Thread.Sleep(5);
}

Console.WriteLine($"\n完成：触发 {fired.Count}/3 个任务。");
if (fired.Count < 3)
{
    Console.WriteLine("有任务未触发，请检查实现。");
    Environment.ExitCode = 1;
}
else
{
    Console.WriteLine("分层时间轮运行正常。");
}
