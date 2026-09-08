using System.Diagnostics;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Demo.AsyncAwait.Console：异步编程演示 ===\n");

await DemoWhenAllAsync();
Console.WriteLine();
await DemoCancellationAsync();
Console.WriteLine();
ExplainSyncOverAsyncDeadlockRisk();

Console.WriteLine("\n全部演示完成。");

static async Task DemoWhenAllAsync()
{
    Console.WriteLine("【1】Task.WhenAll 并发");
    var sw = Stopwatch.StartNew();

    var taskA = SimulateWorkAsync("任务A", 400);
    var taskB = SimulateWorkAsync("任务B", 500);
    var taskC = SimulateWorkAsync("任务C", 300);

    var results = await Task.WhenAll(taskA, taskB, taskC);
    sw.Stop();

    Console.WriteLine($"  结果：{string.Join("、", results)}");
    Console.WriteLine($"  总耗时约 {sw.ElapsedMilliseconds} ms（并发应远小于三者之和 ~1200ms）");
}

static async Task DemoCancellationAsync()
{
    Console.WriteLine("【2】CancellationToken 取消");
    using var cts = new CancellationTokenSource();
    var work = LongRunningAsync(cts.Token);

    // 200ms 后取消
    _ = Task.Run(async () =>
    {
        await Task.Delay(200);
        Console.WriteLine("  -> 发出取消请求");
        cts.Cancel();
    });

    try
    {
        await work;
        Console.WriteLine("  意外：任务未取消");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("  已捕获 OperationCanceledException：任务按预期取消");
    }
}

static void ExplainSyncOverAsyncDeadlockRisk()
{
    Console.WriteLine("【3】同步阻塞导致死锁风险（仅说明，不主动触发死锁）");
    // 风险场景示意（请勿在带 SynchronizationContext 的环境中这样写，例如旧版 ASP.NET / UI 线程）：
    //
    //   async Task<string> GetDataAsync()
    //   {
    //       await Task.Delay(100); // 续体会尝试回到捕获的上下文
    //       return "ok";
    //   }
    //
    //   var result = GetDataAsync().Result; // 或 .Wait() / .GetAwaiter().GetResult()
    //
    // 说明：调用线程被 .Result 阻塞占用上下文；而 await 后续代码又需要回到该上下文才能继续，
    // 于是双方互相等待 → 死锁。控制台/.NET 默认线程池通常不会复现，但 Web/UI 中很常见。
    // 正确做法：整条调用链 async/await 到底，避免同步阻塞异步方法。
    Console.WriteLine("  结论：不要用 .Result / .Wait() 阻塞异步方法；保持 async 一路到底。");
}

static async Task<string> SimulateWorkAsync(string name, int delayMs)
{
    await Task.Delay(delayMs);
    return $"{name}完成({delayMs}ms)";
}

static async Task LongRunningAsync(CancellationToken cancellationToken)
{
    for (var i = 1; i <= 20; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine($"  工作进度 {i}/20");
        await Task.Delay(100, cancellationToken);
    }
}
