# Demo.Inbox.Api

对应笔记：分布式/Inbox · 端口：5106（Development 下 API 文档：http://localhost:5106/scalar）

用内存去重表演示消费端幂等：以 messageId 为键登记已处理事件，重复投递不再执行副作用，失败未登记可安全重投，配合 `?fail=true` 可完整观察重投路径。

## 核心机制
- 首次处理先 `TryAdd` 一条 `Processing` 占位，副作用（计数 +1）完成后才置 `Completed`——占位模拟「去重记录与副作用同事务」。
- 已 `Completed` 的消息重投直接返回 200 且 `duplicated=true`，不重复执行副作用，dedupCount 加一。
- 并发同 messageId 只有一个请求占位成功，其余拿到 409「正在处理中」：用占位状态而非锁解决 check-then-act 竞态；占位失败后回查一次，恰好接住「对方刚好完成」的窗口。
- Controller 先做 `IsProcessed` 快查，再由 `TryProcess` 的占位兜底：快查挡掉绝大多数重复，占位挡掉并发窗口，两级缺一不可。
- `?fail=true` 模拟消费失败：返回 500 且不写 inbox，消息可原样重投，最终副作用仍只执行一次。

## 关键文件
| 文件 | 说明 |
|---|---|
| [InboxAppService.cs](Application/InboxAppService.cs) | Processing/Completed 状态机、TryAdd 占位与失败回滚 |
| [EventsController.cs](Controllers/EventsController.cs) | 接收入口：去重命中、fail 注入、409 分支 |
| [ProcessedEvent.cs](Entities/ProcessedEvent.cs) | inbox 记录与两种状态 |
| [StatsController.cs](Controllers/StatsController.cs) | 三个计数器的对照输出 |

## 运行

```bash
cd Demo.Inbox.Api
dotnet run
```

```bash
# 首次投递：执行副作用（sideEffectCounter +1）
curl -X POST http://localhost:5106/api/events -H 'Content-Type: application/json' -d '{"messageId":"m-1"}'
# 重复投递：duplicated=true，副作用不再执行
curl -X POST http://localhost:5106/api/events -H 'Content-Type: application/json' -d '{"messageId":"m-1"}'
# 对照 processedCount / dedupCount / sideEffectCounter
curl http://localhost:5106/api/stats
```

## 笔记要点
- Inbox 是 Outbox 在消费端的镜像：先登记再执行副作用，用唯一键挡住消息队列的重复投递。
- 去重记录与副作用必须落在同一事务里，否则崩溃窗口会出现「登记了没执行」或「执行了没登记」。
- 409 表示「别人正在处理」，客户端稍后重投即可，不是错误。
