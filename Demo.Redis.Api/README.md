# Demo.Redis.Api

对应笔记：缓存/Redis缓存 · 端口：5113（Development 下 API 文档：http://localhost:5113/scalar）

## 核心机制

- 完整演示 Cache-Aside 读写两条线：读先查缓存，miss 后读 DB 并回填；写只更新 DB 并删除缓存，等下次读自然回填。
- 没有真实 Redis：`MemoryCacheAside` 用 `ConcurrentDictionary` 模拟缓存组件，`TryGet/Set/Remove` 的接口形状与真实客户端一致，把替换成本隔离在 Services 层。
- 模拟 DB 预置 3 条商品（`ProductDb` 单例），缓存键为 `product:{id}`。
- 响应体带 `source` 与 `cacheHit` 字段：同一接口连打两次，能直接看到 db → cache 的切换。
- 命中与未命中用 `Interlocked` 计数，`GET /api/cache/stats` 输出 hits/misses/entries，可手动算命中率。
- 写操作"先更新库、后删缓存"：不回写缓存是为了避免并发写把旧值覆盖回去，反正下次 miss 会重建。

## 关键文件

| 文件 | 说明 |
|---|---|
| [ProductAppService.cs](Application/ProductAppService.cs) | Cache-Aside 核心：读回填、写删缓存 |
| [MemoryCacheAside.cs](Services/MemoryCacheAside.cs) | 内存模拟缓存 + 命中统计 |
| [ProductDb.cs](Services/ProductDb.cs) | 模拟数据库（预置 3 条商品） |
| [ProductsController.cs](Controllers/ProductsController.cs) | 读/写路由，响应标注缓存来源 |
| [CacheController.cs](Controllers/CacheController.cs) | 命中统计端点 |

## 运行

```bash
cd Demo.Redis.Api
dotnet run
```

```bash
curl http://localhost:5113/api/products/1     # 第 1 次：source=db，回填缓存
curl http://localhost:5113/api/products/1     # 第 2 次：source=cache，cacheHit=true
curl -X PUT http://localhost:5113/api/products/1 \
     -H "Content-Type: application/json" \
     -d '{"name":"改名商品","price":29.9}'    # 更新 DB 并删缓存，再 GET 会重新回填
curl http://localhost:5113/api/cache/stats    # hits / misses / entries
```

## 笔记要点

- Cache-Aside 的口诀：读 miss 回填，写"更新 DB + 删缓存"而不是"更新缓存"。
- 删缓存后必有一次 miss 重建，这是该模式与生俱来的代价，换来的是缓存与 DB 的最终一致。
- Demo 的缓存组件接口刻意贴近真实客户端，换成 StackExchange.Redis 时应用层代码几乎不动。
