# Demo.Sso.Api

对应笔记：认证/单点登录 · 端口：5119（Development 下 API 文档：http://localhost:5119/scalar）

## 核心机制

- CAS 风格三步走：`POST /api/sso/login` 换取 SSO 会话（16 字节 hex）→ 携带会话与 `service` 调 `GET /api/sso/ticket` 签发一次性 ticket → 客户端拿 ticket 调 `POST /api/sso/validate` 换出用户名。
- Ticket 为 `ST-` 前缀 + 12 字节随机 hex，TTL 5 分钟，签发即绑定 service。
- Ticket 一次性消费：validate 校验通过后用 `TryUpdate`（携带原 entry 作比较值）把 `Consumed` 置为 true，同一 ticket 第二次 validate 报"已被消费"，重放被挡死。
- Service 白名单：只有 `GET /api/sso/clients` 列出的两个内置假客户端 URL 可签发 ticket，且 validate 时要求 service 与签发时一致，防止 ticket 被挪用到其他站点。
- 演示简化（代码内已注明）：SSO 会话用 query 明传，而非中央域下的会话 cookie；任意非空账号密码均可登录。
- 会话与 ticket 全部存内存字典，服务重启即全部失效。

## 关键文件

| 文件 | 说明 |
|---|---|
| [SsoAppService.cs](Application/SsoAppService.cs) | 登录、ticket 签发、一次性校验（CAS 消费标记） |
| [SsoController.cs](Controllers/SsoController.cs) | `clients` / `login` / `ticket` / `validate` 四个端点 |
| [SsoClient.cs](Entities/SsoClient.cs) | 白名单客户端实体（名称 + 回调 service URL） |
| [SsoDtos.cs](Dtos/SsoDtos.cs) | 各步输入输出 DTO，hint 字段提示下一步动作 |

## 运行

```bash
cd Demo.Sso.Api
dotnet run
```

```bash
SESSION=$(curl -s -X POST http://localhost:5119/api/sso/login -H "Content-Type: application/json" -d '{"username":"alice","password":"x"}' | grep -o '"ssoSessionId":"[^"]*' | cut -d'"' -f4)
TICKET=$(curl -s "http://localhost:5119/api/sso/ticket?service=https://app-a.demo.local/callback&ssoSessionId=$SESSION" | grep -o '"ticket":"[^"]*' | cut -d'"' -f4)
curl -X POST http://localhost:5119/api/sso/validate -H "Content-Type: application/json" -d "{\"ticket\":\"$TICKET\",\"service\":\"https://app-a.demo.local/callback\"}"
```

同一 ticket 再 validate 一次返回"ticket 已被消费"；把 service 换成白名单外的 URL 返回"未知的 service"。

## 笔记要点

- 凭据只交给认证中心一次，各客户端只接触短命一次性 ticket，这是"单点"二字的本质。
- 一次性 + 短 TTL + service 绑定，三道约束共同构成 ticket 的防重放与防盗用边界。
- 真实 CAS 中会话由中央域 cookie 承载、validate 回包写客户端自身会话，本 demo 为可观察性而简化。
