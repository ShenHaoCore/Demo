# Demo.Algorithms.Tests

对应笔记：**算法 / 双指针 · 链表 · 二叉树 · [分层时间轮](https://github.com/ShenHaoCore/hao_notes/blob/main/程序员/知识/04-基础/算法/07-分层时间轮.md)**

xUnit 测试项目，**用 `dotnet test` 运行**（不是 `dotnet run`；测试项目没有可执行演示入口）。

分层时间轮若要看控制台打印，请运行 [`Demo.HierarchicalTimerWheel.Console`](../Demo.HierarchicalTimerWheel.Console)：

```bash
cd ../Demo.HierarchicalTimerWheel.Console
dotnet run
```

## 运行测试

```bash
cd Demo.Algorithms.Tests
dotnet test
```

只跑时间轮：

```bash
dotnet test --filter FullyQualifiedName~HierarchicalTimingWheel
```

当前共 **14** 个测试：算法 8 + 分层时间轮 6。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Algorithms.cs](Algorithms.cs) | 双指针 / 链表 / 二叉树 |
| [AlgorithmTests.cs](AlgorithmTests.cs) | 8 个算法 `[Fact]` |
| [HierarchicalTimingWheel.cs](HierarchicalTimingWheel.cs) | 分层时间轮（滑动窗口 + 粗→细收集再重入） |
| [HierarchicalTimingWheelTests.cs](HierarchicalTimingWheelTests.cs) | 6 个时间轮 `[Fact]` |

## 分层时间轮要点

- 覆盖窗口随 `currentTime` 滑动，不能写死 `StartTime + interval`
- 推进时先各层 Flush，再统一经最细层重入
- `MaxDelay = TickMs × WheelSize^NumLevels`；`CalculateLevels(1,60,24h) = 5`
