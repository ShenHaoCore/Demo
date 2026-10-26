# Demo.CacheProblems.Api

对应笔记：缓存/缓存三大问题 · 端口：5114（Development 下 API 文档：http://localhost:5114/scalar）

## 核心机制

- 一个服务串起穿透/击穿/雪崩三大问题，每个接口都带"关掉解法"的开关，先复现问题再验证解法。
- 穿透：`GET /api/demo/penetration` 对 DB 查不到的 id 也缓存空值（TTL 30 秒），`?cacheNull=false` 时每次未命中都打 DB，模拟恶意不存在 id 的穿透。
- 击穿：热点 key `hot:product` 过期后用 `SemaphoreSlim` 互斥重建（200ms 模拟慢 DB），抢不到锁的请求等 50ms 再查缓存，仍未命中返回 409 提示重试；`?useLock=false` 时并发请求全部涌向 DB。
- 雪崩：`GET /api/demo/avalanche` 给 5 个批量 key 的 TTL = 基础值 + 随机抖动（0 ~ base/2 秒），`?jitter=false` 时全部同 TTL、同一时刻集体过期。
- 过期是懒判断：读时发现 `ExpireAt` 已过即删；`POST /api/demo/cache/expire-hot` 可手动过期热点 key，随时触发击穿演示。
- 模拟 DB 只预置 `1`、`demo` 两条数据，其余 id 一律"不存在"，专门服务穿透演示。

## 关键文件

| 文件 | 说明 |
|---|---|
| [CacheDemoAppService.cs](Application/CacheDemoAppService.cs) | 三大问题主体：空值缓存、互斥重建、TTL 抖动 |
| [DemoController.cs](Controllers/DemoController.cs) | 四个路由与开关参数，击穿冲突返回 409 |
| [CacheItem.cs](Entities/CacheItem.cs) | 缓存条目：是否存在 + 数据 |

## 运行

```bash
cd Demo.CacheProblems.Api
dotnet run
```

```bash
curl "http://localhost:5114/api/demo/penetration?id=notexist"   # 第 1 次查 DB 并缓存空值，之后不再打 DB
curl -X POST http://localhost:5114/api/demo/cache/expire-hot    # 手动过期热点 key
curl "http://localhost:5114/api/demo/breakdown"                 # 只有一个请求重建，其余等待命中或 409
curl "http://localhost:5114/api/demo/avalanche"                 # 看 5 个 key 的 ttlSeconds 各不相同
```

## 笔记要点

- 三大问题的共同根因是"缓存缺失时流量直冲 DB"，区别只在触发条件：不存在的数据 / 单个热点 / 大批 key 同时过期。
- 三个解法都是"软"手段（空值缓存、互斥锁、TTL 抖动），生产还要补布隆过滤器、缓存层高可用等硬手段。
- 互斥重建要处理"拿不到锁怎么办"：短暂等待重查缓存，而不是干等或直接报错。
