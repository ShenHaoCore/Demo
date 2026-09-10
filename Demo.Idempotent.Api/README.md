# Demo.Idempotent.Api

对应笔记：**分布式 / 幂等性**。演示 HTTP 写操作的 `Idempotency-Key`：同 Key 同 Body 回放首次成功响应，同 Key 不同 Body 返回 409；订单与幂等快照在 **同一 DB 事务** 中落库。

- 端口：`5104`
- 框架：.NET 9 + ASP.NET Core Controllers
- 存储：EF Core + SQLite（`idempotent.db`）

## 运行

```bash
cd Demo.Idempotent.Api
dotnet run
```

- Scalar：`http://localhost:5104/scalar`
- 健康检查：`GET http://localhost:5104/health`
- 请求样例：见 `Demo.Idempotent.Api.http`

> Demo 使用 `EnsureCreated`。若改了实体模型，请手动删除 `idempotent.db`（及 `-shm`/`-wal`）后重启。

## 行为一览

| 场景 | 结果 |
|------|------|
| 缺少 `Idempotency-Key` | `400` |
| 首次合法创建 | `201` + `Location`，写入订单与幂等快照 |
| 同 Key + 同 Body（含空白/字段顺序差异） | 回放首次响应（含 `Location`） |
| 同 Key + 不同 Body | `409` |
| 并发同 Key | 唯一约束冲突后回查，回放或冲突 |
| 幂等 TTL 过期 | 仅作废幂等行，**不删历史订单**；同 Key 可再建新单 |

请求头约定：

- 请求：`Idempotency-Key: <client-generated-key>`
- 响应：回写同一 `Idempotency-Key`；成功创建带 `Location`

默认 TTL：`24:00:00`（`appsettings.json` → `Idempotency:Ttl`）。

## 架构（ABP 风格）

```
[Idempotent] Attribute          → 协议（Key / BodyHash）
OrdersController                → CreateOrderResult → HTTP
Application/Orders              → IOrderAppService / OrderAppService
Application/Idempotency         → IIdempotencyRepository + Snapshot/Check*
Data                            → IdempotentDbContext / IdempotencyRepository
```

要点：

1. **Attribute 只管协议**，不做回放/落库。
2. **AppService 先只读查找**；命中则直接 Replay/Conflict，不开写事务。
3. **Miss 时一次 SaveChanges**：清理过期幂等行（若有）+ 写订单 + 写幂等快照（EF 工作单元，无需手写事务）。
4. **权威在 DB**；Redis 不能与 SQL 同事务，仅适合回放缓存。

目录约定：

| 目录 | 职责 |
|------|------|
| `Controllers/` | HTTP API |
| `Application/Orders/` | 订单应用服务与 Result |
| `Application/Idempotency/` | 幂等仓储端口与检查模型 |
| `Data/` | DbContext、仓储实现 |
| `Entities/` / `Dtos/` | 实体与 DTO |
| `Filters/` | `[Idempotent]`、Body 哈希 |
| `Options/` | `IdempotencyOptions` |

## 面试可讲

- 为何写操作要幂等、Key 由谁生成
- Attribute / 中间件 / 业务内嵌 的取舍
- 为何下单场景优先 **DB 同事务**，而不是 Redis SET NX
- TTL 与「历史订单是否删除」的语义区别
- 并发下唯一约束 + 回查回放
- Repository 与 AppService 的职责边界（对齐 ABP）
