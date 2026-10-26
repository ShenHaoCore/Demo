# Demo.Seckill.Api

对应笔记：缓存/秒杀 · 端口：5115（Development 下 API 文档：http://localhost:5115/scalar）

## 核心机制

- 两段式链路：抢购接口只做"原子预扣库存 + 发放票据"，建单由后台 `BackgroundService` 从 `Channel` 无界队列异步消费（30ms 模拟建单耗时），HTTP 请求与建单彻底解耦。
- 预扣用 CAS 自旋：`Volatile.Read` 读余量，`Interlocked.CompareExchange` 抢占扣减，失败就重读重试——"读-判断-扣减"合成原子操作，天然防超卖。
- 余量扣到 0 直接 `SoldOut`（409），不排队，之后的所有请求都进不了扣减分支。
- 抢购成功返回 `ticketId`（状态 Queued），客户端轮询 `GET /api/seckill/orders/{ticketId}` 直到 Built，用"票据"衔接同步响应与异步建单。
- 抢购不传 `userId` 时自动生成随机 8 位用户标识，方便单接口压测。
- 全内存实现（`ConcurrentDictionary` + `Channel`），重启即失；代码注释明确标注"一人一单"未实现，生产需按 activityId + userId 去重。

## 关键文件

| 文件 | 说明 |
|---|---|
| [SeckillAppService.cs](Application/SeckillAppService.cs) | CAS 自旋预扣、票据发放、内存建单队列 |
| [OrderBuilderService.cs](Services/OrderBuilderService.cs) | 后台消费队列异步建单（Queued → Built） |
| [SeckillController.cs](Controllers/SeckillController.cs) | 建活动 / 查活动 / 抢购 / 查票据四个路由 |
| [SeckillEntities.cs](Entities/SeckillEntities.cs) | 活动、票据实体与状态枚举 |

## 运行

```bash
cd Demo.Seckill.Api
dotnet run
```

```bash
curl -X POST http://localhost:5115/api/seckill/activities \
     -H "Content-Type: application/json" -d '{"name":"手机秒杀","stock":5}'   # 201，记下返回的 id
curl -X POST http://localhost:5115/api/seckill/{id}/grab \
     -H "Content-Type: application/json" -d '{"userId":"u1"}'                # 成功返回 ticketId；库存归零后 409
curl http://localhost:5115/api/seckill/orders/{ticketId}                     # 轮询 status: Queued -> Built
```

## 笔记要点

- 防超卖的本质是把"读余量、判断、扣减"合成一个原子步骤：单机用 Interlocked/CAS，分布式换成 Redis Lua 或 DB 原子 UPDATE。
- 削峰靠"预扣 + 异步建单"：瞬时流量只消耗一次原子扣减，建单速率由后台消费能力决定。
- Demo 未做一人一单与票据超时补偿（建单失败的库存回补），是生产化必须补齐的两块。
