# Demo.Algorithms.Tests

对应笔记：**算法 / 双指针 · 链表 · 二叉树** · xUnit 测试项目，`dotnet test` 运行

五个经典算法的自包含实现加 8 个行为化命名的单元测试：有序数组双指针、链表反转与判环、二叉树层序与前序遍历。

## 核心机制

- `TwoSumSorted`（双指针对撞）：有序数组两端收缩，`sum < target` 左指针右移、`sum > target` 右指针左移，O(n) 时间 O(1) 空间；找不到返回 `null`（可空元组）。
- `ReverseList`（链表反转）：`prev / current / next` 三指针迭代原地反转，空链表返回 `null`。
- `HasCycle`（Floyd 判环）：快指针每次走两步、慢指针走一步，`ReferenceEquals(slow, fast)` 相遇即有环；`fast?.Next` 的条件写法天然兼容空链表与单节点。
- `LevelOrder`（BFS 层序）：队列 + 每轮先取 `queue.Count` 快照控制"本层"循环次数，输出 `IList<IList<int>>` 分层结构。
- `Preorder`（前序 DFS）：根 → 左 → 右的递归遍历，与层序形成对照。
- 算法实现与测试同项目自包含（`Algorithms.cs` 无独立 src 工程），测试方法名直接用中文描述行为。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Algorithms.cs](Algorithms.cs) | 五个算法实现与 `ListNode` / `TreeNode` 定义 |
| [AlgorithmTests.cs](AlgorithmTests.cs) | 8 个 `[Fact]` 测试，覆盖正常与边界（空、无解、无环）分支 |

## 运行

```bash
cd Demo.Algorithms.Tests
dotnet test
```

8 个测试全部通过（TwoSumSorted 2 个、ReverseList 2 个、HasCycle 2 个、LevelOrder 与 Preorder 各 1 个）。

## 测试覆盖

| 算法 | 用例 |
|---|---|
| TwoSumSorted | `[1,2,4,7,11,15]` 找 13 命中下标 `(1,4)`；无解返回 `null` |
| ReverseList | 1→4 反转为 4→1；空链表返回 `null` |
| HasCycle | `c.Next = b` 构造环返回 `true`；无环链表与 `null` 返回 `false` |
| LevelOrder / Preorder | 同一棵三层树，分别断言分层结构与 `1,2,4,5,3` 序列 |

## 笔记要点

- 双指针与快慢指针的终止性都来自"指针移动条件"：对撞指针靠有序性，快慢指针靠速度差必然在环内相遇。
- BFS 按层输出的关键动作是每一轮先读一次 `queue.Count`，再固定循环这么多次。
- 断言写行为而非重抄实现：如验证 `nums[left] + nums[right] == target`，而不是在测试里重新算一遍下标。
