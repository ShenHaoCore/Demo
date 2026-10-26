# Demo.DistributedLock.Api

对应笔记：存储/分布式锁 · 端口：5121（Development 下 API 文档：http://localhost:5121/scalar）

## 核心机制

- 用内存 `ConcurrentDictionary` 模拟 Redis `SET key token NX EX`：`TryAdd` 即"不存在才写入"，抢锁失败返回 409"资源已被锁定"。
- 每把锁签发一个 Guid 形态的 token：释放必须携带该 token，不匹配返回 409 拒绝，防止误删他人的锁；查询接口只回 token 前 8 位避免误用。
- 释放用 `TryRemove(KeyValuePair)` 按键值对删除：只有"键和值都还是自己那个"才删得掉，等价 Redis 侧的 compare-and-delete Lua 脚本。
- TTL 到期靠惰性清理：`acquire` / `release` 前都先清过期项；`acquire` 失败后还会再清一次并重试一次 `TryAdd`，兜住"刚好在我尝试瞬间过期"的边界。
- TTL 默认 10 秒，取值被钳制在 1-300 秒——过期时间同时是死锁兜底（持有者崩溃后锁自动可抢）。
- 临界区演示 `POST /api/critical/{resource}`：抢不到锁直接 409 拒绝执行而非排队；持锁期间 `Task.Delay(workMs)`（10-5000ms），`finally` 中释放锁，TTL 内不续期。

## 关键文件

| 文件 | 说明 |
|---|---|
| [InMemoryLockService.cs](Services/InMemoryLockService.cs) | 锁核心：TryAdd 抢锁、带 token 的 compare-and-delete、过期惰性清理 |
| [LockAppService.cs](Application/LockAppService.cs) | 加锁/解锁/查状态的应用服务与响应消息 |
| [CriticalAppService.cs](Application/CriticalAppService.cs) | 持锁执行临界区演示（try/finally 释放） |
| [LocksController.cs](Controllers/LocksController.cs) | `acquire` / `release` / 查询三个端点与 409、404 映射 |
| [LockEntry.cs](Entities/LockEntry.cs) | 锁条目：token + 过期时间 |

## 运行

```bash
cd Demo.DistributedLock.Api
dotnet run
```

```bash
curl -X POST http://localhost:5121/api/locks/order-1/acquire -H "Content-Type: application/json" -d '{"ttlSeconds":10}'
curl -X POST http://localhost:5121/api/locks/order-1/release -H "Content-Type: application/json" -d '{"token":"<acquire 返回的 token>"}'
curl -X POST http://localhost:5121/api/critical/order-1 -H "Content-Type: application/json" -d '{"workMs":1000}'
```

重复 acquire 同一资源返回 409；用错误 token release 返回 409"token 不匹配"；锁不存在返回 404。

## 笔记要点

- 分布式锁三要素：互斥写入（NX）、持有者标识（token）、兜底过期（TTL），缺一就会死锁或误删他人锁。
- "查一下锁存在再 DEL"不是原子的，必须 compare-and-delete；token 的存在正是为了让比较成为可能。
- 与 Demo.OptimisticLock.Api 对照：锁直接拒绝并发（失败即 409），乐观锁把冲突留给客户端重试。
