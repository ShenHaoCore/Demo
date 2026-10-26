# Demo.OrderTimeout.Api

对应笔记：**支付 / 超时关单** · 端口：`5122`（Development 下 API 文档：http://localhost:5122/scalar）

演示"待支付订单超时自动关单"：后台服务定时扫描过期订单执行关单，与用户的支付请求构成**并发竞争**，靠状态 CAS 保证只有一方成功。

## 核心机制

- 订单存在进程内 `ConcurrentDictionary`，实体是 `record`：状态流转用 `current with { ... }` 生成新实例，从不原地改字段。
- `TryPay` / `TryClose` 都是 **CAS 自旋**：`_orders.TryUpdate(id, updated, current)`，仅当订单仍为 `Pending` 才提交，失败则重读重试。
- 后台 `OrderTimeoutService`（`BackgroundService`）每 **500ms** 快照扫描一次，对 `Pending` 且 `ExpireAt` 已过的订单调用 `TryClose("超时自动关单")`。
- **竞态窗口**：扫描判定"该关单"到 CAS 提交之间订单可能已被支付——`TryUpdate` 失败即关单输给支付，日志记录冲突后跳过；反之支付请求拿到 `409 Conflict` + 当前状态。
- 创建订单时 `timeoutSeconds` 可调（默认 15 秒，`Clamp(3, 3600)`），把超时调到 3 秒即可现场观察竞态。
- "超时"用 `ExpireAt <= now` 判定而非延迟队列，把"关单与支付谁先到"的竞争显式化，是本 Demo 的教学核心。

## 关键文件

| 文件 | 说明 |
|---|---|
| [OrderAppService.cs](Application/OrderAppService.cs) | 内存订单仓储 + `TryPay`/`TryClose` 的 CAS 状态机 |
| [OrderTimeoutService.cs](Services/OrderTimeoutService.cs) | 每 500ms 扫描过期 `Pending` 订单的 `BackgroundService` |
| [OrdersController.cs](Controllers/OrdersController.cs) | 创建/查询/支付/手动关单，`409` 携带当前状态 |
| [Order.cs](Entities/Order.cs) | `OrderStatus`（Pending/Paid/Closed）与 `record Order` |

## 运行

```bash
cd Demo.OrderTimeout.Api
dotnet run
```

```bash
# 创建 5 秒超时的待支付订单
curl -s -X POST http://localhost:5122/api/orders \
  -H "Content-Type: application/json" \
  -d '{"product":"键盘","amount":299,"timeoutSeconds":5}'

# 5 秒内支付成功（CAS：Pending -> Paid）
curl -s -X POST http://localhost:5122/api/orders/<id>/pay

# 等 5 秒超时后再操作，得到 409 与当前状态 Closed
curl -s -X POST http://localhost:5122/api/orders/<id>/close
```

## 笔记要点

- 超时关单与支付是并发竞争，正确性来自 `Pending -> Paid` / `Pending -> Closed` 的单向 CAS 流转，而不是"关单前再检查一次状态"。
- `409` + 当前状态让调用方区分"输了竞争（已支付/已关单）"与"订单不存在（404）"。
