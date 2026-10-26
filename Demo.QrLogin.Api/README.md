# Demo.QrLogin.Api

对应笔记：认证/扫码登录 · 端口：5118（Development 下 API 文档：http://localhost:5118/scalar）

## 核心机制

- 票据状态机：`Pending → Scanned → Confirmed`，旁路 `Expired`；每个转移都有前置校验，`scan` 只接受 `Pending`，`confirm` 只接受 `Scanned`，越级操作返回 400 并报出当前状态。
- 票据 TTL 仅 2 分钟，采用惰性过期：任何读写前都先 `MarkExpiredIfNeeded` 把到期票据翻成 `Expired`，不依赖后台清理任务。
- 双端靠同一票据 ID 对齐：PC 端反复 `GET /status` 轮询等确认，"手机端"依次调 `POST /scan`、`POST /confirm`。
- 确认成功生成 24 字节随机数的 Base64 作为登录态 token；`Confirmed` 后 `GET /status` 会把它下发给轮询中的 PC 端。
- 票据 ID 为 16 字节随机数的十六进制串；`scan` 的 username 可以不传，缺省为"演示用户"。
- 票据不存在返回 404，已过期或状态不允许返回 400，全部依赖状态机判定而非防御式 if 散落各处。

## 关键文件

| 文件 | 说明 |
|---|---|
| [QrLoginAppService.cs](Application/QrLoginAppService.cs) | 票据生命周期：创建、扫码、确认、状态查询、惰性过期 |
| [QrTicket.cs](Entities/QrTicket.cs) | `QrStatus` 状态机枚举与票据实体 |
| [QrLoginController.cs](Controllers/QrLoginController.cs) | `create` / `scan` / `confirm` / `status` 四个端点 |
| [QrDtos.cs](Dtos/QrDtos.cs) | 各状态返回 DTO（status 响应带当前阶段的中文提示） |

## 运行

```bash
cd Demo.QrLogin.Api
dotnet run
```

```bash
TICKET=$(curl -s -X POST http://localhost:5118/api/qr/create | grep -o '"ticket":"[^"]*' | cut -d'"' -f4)
curl -X POST http://localhost:5118/api/qr/$TICKET/scan -H "Content-Type: application/json" -d '{"username":"alice"}'
curl -X POST http://localhost:5118/api/qr/$TICKET/confirm
curl http://localhost:5118/api/qr/$TICKET/status
```

`scan` 请求体可整体省略；连续调两次 `confirm` 会因状态机拒绝而返回 400。

## 笔记要点

- 状态机的价值在于把"此刻谁能做什么"收敛进转移条件，新增状态（如已取消）只需扩展枚举与校验。
- 惰性过期省掉定时器，但要求每个入口都先过 `TryGetFresh`，漏一处就会出现"过期票据复活"。
- 本 demo 把登录态 token 直接经公开的 `status` 接口下发；真实系统应由手机端确认后经安全信道换取，避免任何知道票据 ID 的人冒领。
