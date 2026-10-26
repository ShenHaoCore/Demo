# Demo.CircuitBreaker.Api

对应笔记：弹性/熔断 · 端口：5111（Development 下 API 文档：http://localhost:5111/scalar）

## 核心机制

- 手写 `SimpleCircuitBreaker`（`lock` + 三态状态机），不依赖 Polly，为的是把 Closed / Open / HalfOpen 的迁移逻辑看得一清二楚。
- Closed 下连续失败 3 次跳闸：进入 Open 持续 10 秒，期间调用被 `CircuitOpenException` 直接拒绝（快速失败，不再碰下游）。
- Open 到期后转入 HalfOpen：只放行 1 个并发探测，其余请求即使窗口到期也被拒绝——防止恢复瞬间被并发流量再次打挂。
- 探测成功回 Closed（连续失败清零）；探测失败立即重新 Open，避免"半开被打穿"。
- 下游是单例开关 `ForceFail`（默认 true），`POST /api/downstream/config` 可随时切换，无需重启。
- 每个响应都带熔断器快照：state、consecutiveFailures、totalCalls、rejectedCalls 等，状态迁移全程可观测。

## 关键文件

| 文件 | 说明 |
|---|---|
| [SimpleCircuitBreaker.cs](Application/SimpleCircuitBreaker.cs) | 三态状态机：跳闸阈值、Open 时长、HalfOpen 探测名额、计数器 |
| [CallAppService.cs](Application/CallAppService.cs) | 经 `breaker.ExecuteAsync` 包裹下游调用 |
| [CallController.cs](Controllers/CallController.cs) | 熔断/下游异常统一映射为 503 + 熔断器状态 |
| [BreakerController.cs](Controllers/BreakerController.cs) | 查看状态、手动 reset 回 Closed |
| [DownstreamController.cs](Controllers/DownstreamController.cs) | 模拟下游及其 ForceFail 开关 |

## 运行

```bash
cd Demo.CircuitBreaker.Api
dotnet run
```

```bash
# 默认下游故障：前 3 次 503 是"下游故障"，之后 503 是"熔断拒绝"（下游不再被调用）
for i in $(seq 1 5); do curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5111/api/call; done
curl http://localhost:5111/api/breaker                                  # 确认 state=Open 与各项计数
curl -X POST http://localhost:5111/api/downstream/config \
     -H "Content-Type: application/json" -d '{"forceFail":false}'      # 修好下游，等 10 秒半开探测后恢复
```

## 笔记要点

- 熔断解决的是"别再给挂掉的下游添堵"：Open 期间快速失败，把下游从雪崩里解放出来。
- HalfOpen 的并发探测名额（Demo 为 1）是恢复稳定性的关键，放行过多容易二次跳闸。
- 熔断与重试互补：熔断决定"要不要调"，重试决定"调失败怎么办"，两者叠加要小心放大效应。
