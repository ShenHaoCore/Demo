# Demo.Rest.Api

对应笔记：API/REST · 端口：5101（Development 下 API 文档：http://localhost:5101/scalar）

用一个订单资源把 REST 的动词语义完整过一遍：GET / POST / PUT / PATCH / DELETE 各自对应的状态码与请求响应约定，数据放在单例内存字典里，重点全部落在 HTTP 协议层。

## 核心机制
- 状态码语义写在代码里：列表与详情 200，创建 201 + `Location` 响应头，删除成功 204 NoContent，资源不存在一律 404 并带说明消息。
- PUT 与 PATCH 的差异由两个输入 DTO 表达：`UpdateOrderDto` 三字段全必填、整体替换；`PatchOrderDto` 三字段可空，逐字段 `?? existing` 合并，null 即「不修改」。
- 业务校验集中在应用服务：客户名称非空、金额大于 0，失败抛 `ArgumentException`，Controller 不写 try/catch。
- 全局异常处理 `IExceptionHandler`：`ArgumentException` 映射 400 并透传消息，其余映射 500；Development 返回真实异常消息，其他环境返回统一文案。
- 存储为 `ConcurrentDictionary<Guid, Order>`（服务注册为单例），模拟数据库，让 Demo 聚焦协议本身；创建时 `Status` 缺省由服务端赋 `Pending`。

## 关键文件
| 文件 | 说明 |
|---|---|
| [OrdersController.cs](Controllers/OrdersController.cs) | 五个动词的路由、状态码与 404 分支 |
| [OrderAppService.cs](Application/OrderAppService.cs) | CRUD 业务、PUT/PATCH 合并逻辑、实体校验 |
| [OrderDtos.cs](Dtos/OrderDtos.cs) | Create/Update/Patch 输入与输出 DTO，PATCH 字段可空 |
| [GlobalExceptionHandler.cs](Infrastructure/GlobalExceptionHandler.cs) | 异常类型到状态码的统一映射 |
| [Program.cs](Program.cs) | 装配全局异常处理、OpenAPI 与 Scalar |

## 运行

```bash
cd Demo.Rest.Api
dotnet run
```

```bash
# 创建订单（201，Location 指向新资源）
curl -X POST http://localhost:5101/api/orders -H 'Content-Type: application/json' -d '{"customerName":"张三","amount":99.9}'
# 部分更新：只改 status，其余字段不动
curl -X PATCH http://localhost:5101/api/orders/<id> -H 'Content-Type: application/json' -d '{"status":"Paid"}'
# 删除（204；再删一次为 404）
curl -i -X DELETE http://localhost:5101/api/orders/<id>
```

## 笔记要点
- REST 的骨架是「资源 + 动词 + 状态码」：201/204/404 不是装饰，而是客户端判断下一步行为的依据。
- 全量替换（PUT）与部分更新（PATCH）在 DTO 层就要分开设计，不要用一个接口同时兼容两种语义。
- 全局异常处理把领域校验异常统一翻译为 400，Controller 只关心正常路径与 404。
