# Demo.JwtAuth.Api

对应笔记：认证/JWT · 端口：5117（Development 下 API 文档：http://localhost:5117/scalar）

## 核心机制

- 双 token 流转：AccessToken 为 HS256 签名的 JWT，寿命 `300` 秒；RefreshToken 为 32 字节随机数的 Base64，寿命 7 天，只存服务端内存表。
- JWT 载荷只放一个 `ClaimTypes.Name` 声明；JwtBearer 中间件校验 issuer / audience / 签名 / 有效期，`ClockSkew` 从默认 5 分钟收紧到 30 秒。
- Refresh 轮换（rotation）：刷新时先 `TryRemove` 弹出旧 refresh token，成功即签发新的 access + refresh，旧值立即作废；拿同一个 refresh 刷第二次必得 401，天然暴露重放。
- 演示账号体系：任意非空用户名密码都能登录（代码内已注明），重点在 token 的签发与流转，而非账号校验本身。
- 签名密钥为硬编码的 32+ 字节对称字符串，issuer / audience / 密钥全部集中在 `Program.cs`，生产环境应改走配置或 KMS。
- 与 Demo.PasswordAuth.Api 对照：这里 `GET /api/me` 只挂 `[Authorize]`，鉴权由框架依据 JWT 声明完成，无需查任何存储。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Program.cs](Program.cs) | JwtBearer 配置：校验参数、ClockSkew、对称签名密钥 |
| [JwtTokenService.cs](Services/JwtTokenService.cs) | 签发 HS256 access token（300 秒，含 Name 声明） |
| [AuthAppService.cs](Application/AuthAppService.cs) | 登录与 refresh 一次性轮换的内存实现 |
| [AuthController.cs](Controllers/AuthController.cs) | `login` / `refresh` 端点与错误映射（400 / 401） |
| [MeController.cs](Controllers/MeController.cs) | `[Authorize]` 保护的当前用户接口 |

## 运行

```bash
cd Demo.JwtAuth.Api
dotnet run
```

```bash
curl -X POST http://localhost:5117/api/auth/login -H "Content-Type: application/json" -d '{"username":"alice","password":"x"}'
curl -X POST http://localhost:5117/api/auth/refresh -H "Content-Type: application/json" -d '{"refreshToken":"<上一步返回的 refreshToken>"}'
curl http://localhost:5117/api/me -H "Authorization: Bearer <accessToken>"
```

refresh token 无效、过期或已被轮换消费，均返回 `401`；5 分钟不换新 access token 后访问 `/api/me` 同样 401。

## 笔记要点

- Access 短命 + Refresh 长命是折中产物：无状态校验换来低验证成本，短寿命限制泄露后的影响窗口。
- Refresh 一次性轮换让"同一个 token 被用两次"成为可检测的重放信号，这是无状态 JWT 下少有的自愈手段。
- JWT 一旦签发无法主动作废，登出/封号只能靠短寿命 + 黑名单补偿——这也是上一站会话 token 方案仍然存在的理由。
