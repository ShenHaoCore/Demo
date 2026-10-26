# Demo.PasswordAuth.Api

对应笔记：认证/密码认证 · 端口：5116（Development 下 API 文档：http://localhost:5116/scalar）

## 核心机制

- 密码从不明文落库：PBKDF2（`Rfc2898DeriveBytes.Pbkdf2`，SHA256）派生 32 字节哈希，用户记录里只有 `Username / Salt / PasswordHash` 三个字段。
- 每个用户独立 16 字节随机盐（`RandomNumberGenerator.GetBytes(16)`）：同密码不同用户的哈希完全不同，彩虹表失效；盐本身可明文存储。
- 迭代次数 `100_000`：刻意抬高单次猜测成本，改这个数字能直观感受登录耗时与暴力破解代价的换算关系。
- 哈希比对用 `CryptographicOperations.FixedTimeEquals`：恒定时间比较、不因前缀相同提前返回，防时序侧信道。
- 登录成功签发 32 字节随机数的 Base64 会话 token，存内存 `token -> username` 会话表（服务重启即全员下线）；用户名键按 `OrdinalIgnoreCase` 去重，重复注册返回 409。
- `GET /api/me` 手工解析 `Authorization: Bearer` 头再查会话表，无 token 或无效 token 统一返回 401，不区分"用户不存在"与"密码错误"。

## 关键文件

| 文件 | 说明 |
|---|---|
| [UserAppService.cs](Application/UserAppService.cs) | 注册、登录、会话查询：加盐、PBKDF2、恒定时间比对 |
| [AuthController.cs](Controllers/AuthController.cs) | `register` / `login` / `me` 三个端点与 Bearer 头解析 |
| [UserRecord.cs](Entities/UserRecord.cs) | 用户实体：只存用户名、盐、哈希 |
| [AuthDtos.cs](Dtos/AuthDtos.cs) | 注册与登录输入 DTO |

## 运行

```bash
cd Demo.PasswordAuth.Api
dotnet run
```

```bash
curl -X POST http://localhost:5116/api/register -H "Content-Type: application/json" -d '{"username":"alice","password":"P@ssw0rd"}'
curl -X POST http://localhost:5116/api/login -H "Content-Type: application/json" -d '{"username":"alice","password":"P@ssw0rd"}'
curl http://localhost:5116/api/me -H "Authorization: Bearer <login 返回的 sessionToken>"
```

重复注册同一用户名返回 `409`；`GET /health` 可做探活。

## 笔记要点

- 加盐解决彩虹表，迭代次数解决单次猜测成本，恒定时间比较解决时序攻击，三者缺一不可。
- 失败响应要模糊化：统一 401 能避免向攻击者泄露"哪个用户名存在"。
- 会话 token 方案每次校验都要查存储，是有状态认证；这正是下一站 JWT 无状态方案的对照面。
