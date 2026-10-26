# Demo.OptimisticLock.Api

对应笔记：存储/乐观锁 · 端口：5120（Development 下 API 文档：http://localhost:5120/scalar）

## 核心机制

- 版本号即锁：`Product` 为不可变记录（`init` 属性），每次更新都构造 `Version + 1` 的新实例，从不原地修改，旧版本天然可作比较基准。
- CAS 更新循环：读出 current → 比对请求里的 `expectedVersion` → 用 `ConcurrentDictionary.TryUpdate(id, updated, current)` 提交（键值都匹配才写入）；失败说明有人抢先修改，回到循环重读，直到成功或版本不符。
- 版本不符不自动重试，直接返回 409 并附上当前 `version` 与 `stock`，把"怎么处理冲突"的决定权交回客户端。
- 用 `TryUpdate` 模拟数据库的 `UPDATE ... SET ... WHERE version = @expected`：带条件写入等价于 compare-and-set，冲突时影响行数为 0。
- 预置两件商品（id 1、2，初始 Version 均为 1），更新成功的响应带递增后的新 version，可直接用旧 version 重放观察 409。
- 全内存实现，无数据库；工程上 EF Core 对应 `IsConcurrencyToken` 或 SQL Server 的 `RowVersion`。

## 关键文件

| 文件 | 说明 |
|---|---|
| [ProductAppService.cs](Application/ProductAppService.cs) | CAS 更新循环与冲突结果分类（Success / Conflict / NotFound） |
| [Product.cs](Entities/Product.cs) | 不可变商品实体，含 `long Version` |
| [ProductsController.cs](Controllers/ProductsController.cs) | `GET /api/products/{id}`、`PUT /api/products/{id}/stock`、`POST` 与 409 响应体 |
| [ProductDtos.cs](Dtos/ProductDtos.cs) | `UpdateStockDto`（stock + expectedVersion）等输入输出 |

## 运行

```bash
cd Demo.OptimisticLock.Api
dotnet run
```

```bash
curl http://localhost:5120/api/products/1
curl -X PUT http://localhost:5120/api/products/1/stock -H "Content-Type: application/json" -d '{"stock":90,"expectedVersion":1}'
curl -X PUT http://localhost:5120/api/products/1/stock -H "Content-Type: application/json" -d '{"stock":80,"expectedVersion":1}'
```

第一条 PUT 成功且 version 变 2；第二条用同样的 `expectedVersion=1` 重放，返回 409 并带 `currentVersion: 2`。

## 笔记要点

- 乐观锁不加锁，赌冲突稀少：读多写少时吞吐最高，代价是冲突方必须自己重试或放弃。
- `expectedVersion` 的语义是"我基于那一刻的数据做的决定"，用过期版本号提交必然被拒，这是防丢失更新的根。
- 与 Demo.DistributedLock.Api 对照：乐观锁把冲突暴露给客户端，悲观/分布式锁则直接把并发挡在门外。
