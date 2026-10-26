# Demo.AsyncAwait.Console

对应笔记：**CSharp / async与await** · 控制台程序，直接 `dotnet run` 观察输出

三个递进场景：`Task.WhenAll` 并发计时、`CancellationToken` 协作式取消、"同步阻塞异步"的死锁原理（只讲不演）。

## 核心机制

- **WhenAll 并发**：三个 `Task.Delay`（400/500/300ms）同时启动后 `await Task.WhenAll`，`Stopwatch` 实测总耗时接近最长的 500ms 而不是相加的 1200ms——并发的价值一眼可见。
- **协作式取消**：后台任务 200ms 后 `cts.Cancel()`；工作循环每 100ms 调 `ThrowIfCancellationRequested()` 并用 `Task.Delay(100, token)` 睡眠，最终捕获 `OperationCanceledException` 收尾。取消是请求不是命令，token 不传进去就取消不了；带 token 的 `Task.Delay` 会在取消时立即以取消结束，而不是傻等满 100ms。
- **死锁只讲不演**：`.Result` / `.Wait()` 阻塞调用线程并占用上下文，`await` 的续体又要回到同一上下文，双方互相等待。控制台 / 默认线程池没有 `SynchronizationContext` 复现不了；旧版 ASP.NET、UI 线程上才真死锁，因此 Demo 只输出注释讲解不主动触发。
- 结论一句话：整条调用链 `async/await` 到底，不要用 `.Result` / `.Wait()` / `GetAwaiter().GetResult()` 阻塞异步方法。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Program.cs](Program.cs) | 三个演示场景与死锁风险的注释讲解，全部单文件实现 |

## 运行

```bash
cd Demo.AsyncAwait.Console
dotnet run
```

无 HTTP 接口；运行后按【1】【2】【3】三段输出观察并发耗时、取消过程与死锁结论。

## 输出观察点

- 【1】结果数组按传入顺序返回（A、B、C），与任务完成的先后无关；总耗时一行应显示约 500ms 量级（取最长的任务 B），而三者串行相加约为 1200ms。
- 【2】进度大约打到 2/20 时出现"发出取消请求"，随即抛出并捕获 `OperationCanceledException`，打印"任务按预期取消"；若把 token 从 `Task.Delay(100, token)` 去掉，取消会推迟到下一轮 `ThrowIfCancellationRequested()` 才生效。
- 【3】本段不执行任何阻塞调用，只有注释中的风险代码示意与文字结论，避免在无法复现死锁的环境里误导读死。

## 笔记要点

- 并发耗时趋近 max 而非 sum，`Task.WhenAll` 是并发 IO 的基本盘。
- 取消必须合作：`CancellationToken` 一路透传并定期检查，否则只是"发了个没人听的信号"。
- 顶层语句里直接 `await DemoWhenAllAsync()`，编译器生成的是返回 `Task` 的异步 Main——入口自己就是异步的，无需 `.Wait()`。
- 死锁的根因是"阻塞 + 续体回归上下文"互等；控制台复现不了不代表没有风险。
