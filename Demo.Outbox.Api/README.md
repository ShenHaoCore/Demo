# Demo.Outbox.Api

对应笔记：分布式/Outbox · 端口：5105（Development 下 API 文档：http://localhost:5105/scalar）

用内存结构模拟「订单与待发消息在同一事务落库」：下单接口只写本地状态，真正的对外发送由后台服务读 outbox 完成，消解「写库成功、发消息失败」的双写问题。

## 核心机制
- 内存模拟同事务落库：`CreateOrderWithOutboxAsync` 在同一把锁内先写订单再写 outbox 消息，第二步失败则回滚第一步（移除订单）并抛异常，Controller 返回 409，对应真实场景里一个 DB 事务写两张表。
- 消息状态机 `Pending → Publishing → Published`：`ClaimPending` 抢占一批消息改为 `Publishing`，发送成功后 `MarkPublished` 记录 `PublishedAt`，`GET /api/outbox` 可观察全程。
- 发布由 `OutboxPublisherService`（BackgroundService）每秒轮询，单批最多 20 条；「投递到 MQ」目前只是打日志，替换为真实 MQ 客户端不影响上游代码。
- 业务接口不出现任何 MQ 依赖：下单只写订单和 outbox，消息载荷是序列化的 JSON 事件（`OrderCreated`），发送变成后台读表的副作用。
- 抢占（检查 Pending 并改状态）也在同一把锁内完成，模拟 DB 层 `UPDATE ... WHERE status='Pending'` 的原子语义。

## 关键文件
| 文件 | 说明 |
|---|---|
| [OutboxAppService.cs](Application/OutboxAppService.cs) | 内存事务写订单+outbox，Claim/Mark 状态流转 |
| [OutboxPublisherService.cs](Services/OutboxPublisherService.cs) | 轮询式发布后台服务 |
| [OutboxMessage.cs](Entities/OutboxMessage.cs) | 消息实体与 Pending/Publishing/Published 三态 |
| [OrdersController.cs](Controllers/OrdersController.cs) | 下单入口，同事务写入与 409 回滚分支 |
| [OutboxController.cs](Controllers/OutboxController.cs) | 观察消息状态 |

## 运行

```bash
cd Demo.Outbox.Api
dotnet run
```

```bash
# 创建订单，同时写入一条 OrderCreated outbox 消息
curl -X POST http://localhost:5105/api/orders -H 'Content-Type: application/json' -d '{"product":"键盘","quantity":2}'
# 查询消息状态；约 1 秒后重查，Pending 变 Published
curl http://localhost:5105/api/outbox
# 已创建的订单列表
curl http://localhost:5105/api/orders
```

## 笔记要点
- Outbox 把「发送消息」拆成本地事务内的写表加后台轮询，用两次本地写换掉一次不可靠的跨系统写。
- 代价是至少一次投递，消费端必须幂等（与 Demo.Inbox.Api 配对食用）。
- `Publishing` 占位对应真实实现里的「先标记再发送」；进程崩溃后需要超时重claim，本 Demo 未实现该恢复路径。
