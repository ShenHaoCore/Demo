# Demo.RateLimit.Api

对应笔记：弹性/限流 · 端口：5109（Development 下 API 文档：http://localhost:5109/scalar）

## 核心机制

- 基于 ASP.NET Core 内置限流器（`AddRateLimiter` + `[EnableRateLimiting]`），计数全在单进程内存里，重启即清零。
- 定义 `fixed` / `sliding` 两种策略，同为 10 秒窗口 5 次配额，按 `RemoteIpAddress` 分区：每个 IP 独立计数，互不影响。
- 固定窗口按整窗口重置配额，窗口边界可能出现"两个窗口背靠背"的双倍突发；滑动窗口把窗口切 5 段按比例回收配额，限流更平滑。
- `QueueLimit = 0`：超配额不排队直接拒绝；`AutoReplenishment = true` 让配额随时间自动补充。
- 被限流统一走 `OnRejected`：返回 429，带 `Retry-After` 头（取自限流器元数据）与 JSON 提示。
- `/api/requests/unlimited` 不挂限流策略作对照组；健康检查 `GET /health` 同样不受限。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Program.cs](Program.cs) | 注册限流器：fixed/sliding 策略、IP 分区、429 + Retry-After 定制 |
| [RequestsController.cs](Controllers/RequestsController.cs) | 三个端点：无限制、固定窗口、滑动窗口 |

## 运行

```bash
cd Demo.RateLimit.Api
dotnet run
```

```bash
# 连打 6 次固定窗口：前 5 次 200，第 6 次 429（带 Retry-After），10 秒后配额恢复
for i in $(seq 1 6); do curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5109/api/requests/fixed; done
# 对照组：无限制端点永远 200；滑动窗口被拒的时间点更"分散"
curl http://localhost:5109/api/requests/unlimited
curl http://localhost:5109/api/requests/sliding
```

## 笔记要点

- 固定窗口实现最简单但有边界突发问题；滑动窗口用少量内存换平滑，两者没有绝对优劣。
- 限流键决定"限的是谁"：Demo 按 IP 分区，生产更常见用户 ID、API Key 或租户维度。
- 拒绝响应必须带 `Retry-After`，否则客户端盲目重试会放大流量。
