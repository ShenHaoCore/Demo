# Demo.Algorithms.Tests

对应笔记：**算法 / 双指针 · 链表 · 二叉树**

xUnit 测试项目，用 `dotnet test` 运行。

分层时间轮已独立为 [`Demo.TimingWheel.Console`](../Demo.TimingWheel.Console)（`dotnet run` + `dotnet test`）。

## 运行

```bash
cd Demo.Algorithms.Tests
dotnet test
```

8 个测试：TwoSumSorted / ReverseList / HasCycle / LevelOrder / Preorder。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Algorithms.cs](Algorithms.cs) | 五个算法实现与 `ListNode` / `TreeNode` |
| [AlgorithmTests.cs](AlgorithmTests.cs) | 8 个 `[Fact]` |
