# Demo.HierarchicalTimerWheel.Console

对应笔记：**[分层时间轮](https://github.com/ShenHaoCore/hao_notes/blob/main/程序员/知识/04-基础/算法/07-分层时间轮.md)**

可 `dotnet run` 的分层时间轮演示：调度 80 / 150 / 220 ms 任务，轮询推进时钟并打印到期。

单测在 [`Demo.Algorithms.Tests`](../Demo.Algorithms.Tests)（`HierarchicalTimingWheelTests`）。

## 运行

```bash
cd Demo.HierarchicalTimerWheel.Console
dotnet run
```

## 单测

```bash
cd ../Demo.Algorithms.Tests
dotnet test --filter FullyQualifiedName~HierarchicalTimingWheel
```
