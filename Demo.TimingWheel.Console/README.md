# Demo.TimingWheel.Console

对应笔记：[分层时间轮](https://github.com/ShenHaoCore/hao_notes/blob/main/程序员/知识/04-基础/算法/07-分层时间轮.md)

独立主题：实现 + `dotnet run` + `dotnet test`。

```bash
cd Demo.TimingWheel.Console
dotnet run   # 80/150/220ms 到期打印
dotnet test  # 6 项
```

| 文件 | 说明 |
|------|------|
| `HierarchicalTimingWheel.cs` | 分层轮（滑动窗口、粗→细收集再重入） |
| `Program.cs` | 演示 |
| `TimingWheelTests.cs` | 单测 |
