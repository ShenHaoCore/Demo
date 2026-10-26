# Demo.Retry.Api

对应笔记：弹性/重试 · 端口：5110（Development 下 API 文档：http://localhost:5110/scalar）

## 核心机制

- 用 Polly v8 手工构建 `ResiliencePipeline`（非 `AddResilienceHandler` 管道），在应用服务里显式 `ExecuteAsync` 包裹下游调用。
- 失败按"结果"建模而非异常：下游返回 `Success=false`，靠 `ShouldHandle.HandleResult(r => !r.Success)` 捕获结果型失败再重试。
- 重试参数：最多 4 次重试（含首次共至多 5 次调用），指数退避从 200ms 起步并叠加 `UseJitter` 随机抖动，避免重试同步踩踏。
- `UnstableDownstream` 单例模拟下游：按可配置失败率（默认 0.7）用 `Random.Shared` 随机返回失败，纯内存模拟，无需真实网络。
- `/api/unstable` 直连下游（失败 503），`/api/proxy` 经重试管道，响应带 `attempts` 字段可数出真实调用次数。
- `OnRetry` 回调输出每次重试的等待毫秒数日志，肉眼可见指数退避曲线。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Program.cs](Program.cs) | 构建重试管道：次数、指数退避、Jitter、结果谓词、重试日志 |
| [UnstableDownstream.cs](Services/UnstableDownstream.cs) | 可调失败率的模拟下游（默认 70% 失败） |
| [RetryDemoAppService.cs](Application/RetryDemoAppService.cs) | 直连 / 配置 / 代理重试三个用例，统计 attempts |
| [UnstableController.cs](Controllers/UnstableController.cs) | 对应 HTTP 路由，直连失败返回 503 |

## 运行

```bash
cd Demo.Retry.Api
dotnet run
```

```bash
curl http://localhost:5110/api/unstable                          # 直连：大概率 503（默认失败率 0.7）
curl -X POST http://localhost:5110/api/unstable/config \
     -H "Content-Type: application/json" -d '{"failureRate":0.5}' # 调低失败率
curl http://localhost:5110/api/proxy                             # 经重试代理：看 attempts 与 success
```

## 笔记要点

- 重试只适合"偶发失败"：每次重试都在放大下游压力，指数退避 + Jitter 是防重试风暴的基本功。
- 结果型失败（如 503 响应体）也要写进 `ShouldHandle`，否则管道不会重试，这是 Polly 最常见的踩坑点。
- 最多 5 次调用意味着最坏情况延迟显著变长；对有副作用的写操作，重试必须配幂等。
