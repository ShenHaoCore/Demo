# Demo.Linq.Console

对应笔记：**CSharp / LINQ** · 控制台程序，直接 `dotnet run` 观察输出

用一组"商品 × 库存"数据演示最常用的四个算子（Where / Select / GroupBy / Join），以及最容易踩坑的**延迟执行**。

## 核心机制

- `Where` 过滤、`Select` 投影是最基础的两步；数据源是文件底部定义的 `record Product` / `record Stock` 数组。
- `GroupBy` 按分类分组后，组本身也是可枚举的：直接 `g.Count()` / `g.Average(x => x.Price)` 做每分组聚合。
- `Join` 是内连接：`products.Join(stocks, p => p.Id, s => s.ProductId, (p, s) => new { ... })`，键不匹配的行不会出现在结果里。
- **延迟执行**：`Where` 只构造查询不执行；构造后向源 `List` 追加一个 5，`foreach` 时它也被筛出来——查询在每次枚举时对"当时的源"求值。
- 谓词里打印"正在筛选 x"让执行时机可见：构造查询那行代码执行时没有任何输出，`foreach` 才开始逐个筛选。
- 一句话结论：LINQ 查询是"配方"不是"结果"，想要快照就 `ToList()`。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Program.cs](Program.cs) | 五段演示与文件底部的 `record` 数据模型，单文件实现 |

## 运行

```bash
cd Demo.Linq.Console
dotnet run
```

无 HTTP 接口；运行后按【Where】【Select】【GroupBy】【Join】【延迟执行】五段观察输出。

## 输出观察点

- 【Where】只筛出价格大于 100 的三件：笔记本、键盘、书桌。
- 【GroupBy】每个分类先打汇总行（件数、均价保留两位小数），再缩进列出组内商品。
- 【Join】六件商品按 Id 与库存一一配对，逐行输出"单价 + 库存"。
- 【延迟执行】"（此时尚未执行 Where 谓词）"之后追加源元素 5，再 `foreach`——"正在筛选 x"的打印从枚举时才开始，且 1、3、5 三个奇数全部筛出，包括枚举前新增的 5。

## 笔记要点

- 延迟执行意味着查询定义后源集合的变化会反映到枚举结果中；同一查询多次枚举也会重复执行。
- `GroupBy` 的组不只是键，还能继续聚合；`Join` 默认内连接，需要左连接时是 `GroupJoin` + `DefaultIfEmpty` 的组合。
