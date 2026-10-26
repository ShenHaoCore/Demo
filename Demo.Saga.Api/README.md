# Demo.Saga.Api

对应笔记：分布式/Saga · 端口：5108（Development 下 API 文档：http://localhost:5108/scalar）

用「订机票 → 订酒店 → 租车」行程演示编排式 Saga：任一步失败，已完成的步骤逆序补偿、未执行的标记跳过，一个请求内即可看到完整的状态机流转。

## 核心机制
- 编排式（orchestration）Saga 的内存最小版：`POST /api/trips` 在一个请求内同步跑完三步，`Task.Delay` 模拟远程调用耗时。
- 步骤状态机 `Pending → Succeeded / Failed`：某步失败后，已完成步骤从补偿栈逆序弹出执行补偿（置 `Compensated`），仍是 `Pending` 的统一标记 `Skipped`。
- 行程级状态由失败位置决定：第一步失败且无可补偿 → `Failed`；中途失败并完成补偿 → `Compensated`；全部成功 → `Completed`。
- `Skipped` 与 `Compensated` 的区分是关键：失败点之后的步骤从未发生，不该被补偿。
- `failFlight` / `failHotel` / `failCar` 查询参数注入失败；响应状态码与 Saga 结果绑定：Completed 返回 201，其余返回 409。
- `Trip` / `SagaStep` 为 record，返回前做防御性 Clone，避免调用方改写内存中的状态机。

## 关键文件
| 文件 | 说明 |
|---|---|
| [TripAppService.cs](Application/TripAppService.cs) | 编排主流程、补偿栈、Skipped 标记 |
| [Trip.cs](Entities/Trip.cs) | TripStatus / StepStatus 两层状态机定义 |
| [TripsController.cs](Controllers/TripsController.cs) | 同步编排入口，201/409 状态码语义 |
| [CreateTripDto.cs](Dtos/CreateTripDto.cs) | 输入 DTO 与 SagaFailOptions 失败注入 |

## 运行

```bash
cd Demo.Saga.Api
dotnet run
```

```bash
# 全部成功 → 201 Completed
curl -X POST http://localhost:5108/api/trips -H 'Content-Type: application/json' -d '{"destination":"东京","traveler":"张三"}'
# 租车失败 → 409 Compensated，机票酒店已逆序取消
curl -X POST 'http://localhost:5108/api/trips?failCar=true' -H 'Content-Type: application/json' -d '{"destination":"东京"}'
# 查看每步状态与明细（如「已补偿：取消订机票」「因前序失败未执行」）
curl http://localhost:5108/api/trips/<id>
```

## 笔记要点
- Saga 用「本地事务 + 业务补偿」替代跨服务分布式事务，补偿必须是业务上的逆操作（退票、取消酒店）。
- 补偿只针对已完成的步骤，未执行的是 Skipped——搞混这两者会出现「补偿未发生的事」的 bug。
- 编排式由中心协调器驱动（本 Demo 即应用服务），协同式靠事件链传递；前者流程直观，后者耦合更低。
