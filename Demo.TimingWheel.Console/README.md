# Demo.TimingWheel.Console

对应笔记：**[分层时间轮](https://github.com/ShenHaoCore/hao_notes/blob/main/程序员/知识/04-基础/算法/07-分层时间轮.md)**

独立时间轮主题：实现 + 控制台演示 + xUnit 单测同目录（与 `Demo.Algorithms.Tests` 解耦）。

## 运行演示

```bash
cd Demo.TimingWheel.Console
dotnet run
```

调度 80 / 150 / 220 ms 任务，推进时钟并打印到期。

## 运行单测

```bash
cd Demo.TimingWheel.Console
dotnet test
```

6 个用例：短延迟、跨层降级、MaxDelay、多任务顺序、层数推导、真实时钟冒烟。

## 关键文件

| 文件 | 说明 |
|------|------|
| [HierarchicalTimingWheel.cs](HierarchicalTimingWheel.cs) | 分层时间轮（滑动窗口；粗→细收集再经最细层重入） |
| [Program.cs](Program.cs) | `dotnet run` 演示 |
| [TimingWheelTests.cs](TimingWheelTests.cs) | xUnit 单测 |

## 要点

- `MaxDelay = TickMs × WheelSize^NumLevels`
- 覆盖窗口随 `currentTime` 滑动，不能写死构造时的 `EndTime`
- 推进时先各层 Flush，再统一经最细层重入
