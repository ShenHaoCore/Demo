# Demo.ShortUrl.Api

对应笔记：缓存/短网址 · 端口：5112（Development 下 API 文档：http://localhost:5112/scalar）

## 核心机制

- 短码用 `RandomNumberGenerator` 从 62 字符表随机取 7 位生成，`TryAdd` 撞码就在 do-while 里重试——用唯一性换"短码不可预测、不暴露序号"。
- 存储为单进程 `ConcurrentDictionary`：无 DB、无 Redis，重启后短链与计数全部归零，聚焦短码生成与跳转链路本身。
- 创建前用 `Uri.TryCreate` 校验 `longUrl` 必须是 http/https 绝对地址，非法输入返回 400。
- 跳转是根路由 `GET /{code}`：命中后 `VisitCount++` 再 302 到原始地址；`api` 前缀与 `health/openapi/scalar` 被保留，不当作短码解析。
- 访问计数只是普通 `++`，并发下可能少计；高并发计数应交给 Redis INCR 这类原子操作。
- 302 临时跳转保证每次都经过服务端，访问统计不漏；301 会被浏览器永久缓存导致漏计。

## 关键文件

| 文件 | 说明 |
|---|---|
| [ShortUrlAppService.cs](Application/ShortUrlAppService.cs) | 短码生成（随机 + 撞码重试）、URL 校验、访问计数 |
| [RedirectController.cs](Controllers/RedirectController.cs) | 根路由 302 跳转与保留字过滤 |
| [UrlsController.cs](Controllers/UrlsController.cs) | 创建短链与详情查询 |
| [ShortUrlEntry.cs](Entities/ShortUrlEntry.cs) | 短链实体：code / longUrl / createdAt / visitCount |

## 运行

```bash
cd Demo.ShortUrl.Api
dotnet run
```

```bash
curl -X POST http://localhost:5112/api/urls \
     -H "Content-Type: application/json" \
     -d '{"longUrl":"https://learn.microsoft.com/dotnet"}'   # 201，返回 code 与 shortPath
curl http://localhost:5112/api/urls/{code}                   # 查详情与 visitCount
curl -i http://localhost:5112/{code}                         # 302，Location 指向原始地址
```

## 笔记要点

- 短码生成两条路线：自增 ID + Base62（需发号器、可被遍历）与随机码（不可预测、靠冲突重试），Demo 选后者。
- 跳转是典型读多写少场景，生产应在前面加缓存（Redis/CDN），DB 只兜底。
- 301 还是 302 的本质取舍：永久缓存省流量 vs 每次回源保统计。
