# Demo.MqConsumer.Api

对应笔记：分布式/消息消费 · 端口：5107（Development 下 API 文档：http://localhost:5107/scalar）

用内存结构扮演消息 Broker，完整走一遍消费侧生命周期：发布、出队处理、手动 ACK、重试计数、超限进死信队列，`failUntilAttempt` 一个参数即可模拟毒消息。

## 核心机制
- 内存 Broker：`ConcurrentQueue` 存就绪消息 id、字典存消息本体与死信，`POST /api/messages` 即发布并返回 202 Accepted，无需外部 MQ。
- 消费者 `MessageConsumerService`（BackgroundService）每 300ms 拉一条：`TryDequeue` 把消息置为 `Processing` 并将 AttemptCount 加一，对应真实 MQ 的 unacked 占位。
- 手动 ACK 语义：成功 `AckSuccess` 置 `Succeeded`；失败 `AckFailure` 未达上限则改回 `Queued` 重新入队，达到 `MaxAttempts`（3 次，含首次）写入死信队列不再重试。
- `failUntilAttempt` 让前 N 次必失败，一条消息就能观察重试计数、最终成功或进 DLQ 的完整路径。
- `TryDequeue` 会跳过队列里已 `Succeeded` / `DeadLetter` 的陈旧 id，重试消息复用原 id 重新入队，不产生重复条目。

## 关键文件
| 文件 | 说明 |
|---|---|
| [MessageAppService.cs](Application/MessageAppService.cs) | Broker 核心：入队、出队、ACK、死信转移 |
| [MessageConsumerService.cs](Services/MessageConsumerService.cs) | 消费循环与手动 ACK 决策 |
| [BrokerMessage.cs](Entities/BrokerMessage.cs) | 消息状态机与尝试计数字段 |
| [MessagesController.cs](Controllers/MessagesController.cs) | 发布消息与队列/死信观测端点 |

## 运行

```bash
cd Demo.MqConsumer.Api
dotnet run
```

```bash
# 第 1 次失败，重试后第 2 次成功
curl -X POST http://localhost:5107/api/messages -H 'Content-Type: application/json' -d '{"payload":"hello","failUntilAttempt":1}'
# 每次都失败，3 次后进死信队列
curl -X POST http://localhost:5107/api/messages -H 'Content-Type: application/json' -d '{"payload":"poison","failUntilAttempt":99}'
# 查看死信与队列快照
curl http://localhost:5107/api/dlq
curl http://localhost:5107/api/queue
```

## 笔记要点
- 重试必须有上限（含首次 3 次），到顶进 DLQ，毒消息不能无限占用队列。
- 出队计数与 ACK 分离对应真实 MQ 的 unacked 语义：处理中不丢、也不再投给其他消费者。
- 本 Demo 失败后立即重投、没有退避；真实系统应加指数退避与抖动。
