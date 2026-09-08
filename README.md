# Demo

基于 `haonotes` 面试笔记的 **C# 独立 Demo 集合**。命名风格参考原仓库 `E:\Repos\Demo1`（`Demo.{主题}.Api`），但：

- 落在仓库 `E:\Repos\Demo`（由 NotesDemo 更名）
- **每个主题一个独立目录**，自带 `.sln`，互不引用、无 Shared 项目
- 端口从 **5101** 起，避免与 `Demo1`（5066–5084）冲突

## 环境

- .NET SDK 9.0+
- Windows / Linux / macOS

## 项目一览

| 项目 | 端口 | 对应笔记 | 说明 |
|------|------|----------|------|
| [Demo.Rest.Api](Demo.Rest.Api) | 5101 | API/REST | HTTP 动词与资源设计 |
| [Demo.Versioning.Api](Demo.Versioning.Api) | 5102 | API/API版本化 | URL / Header 版本 |
| [Demo.ETag.Api](Demo.ETag.Api) | 5103 | API/ETag | If-Match 乐观并发 |
| [Demo.Idempotent.Api](Demo.Idempotent.Api) | 5104 | 分布式/幂等性 | Idempotency-Key |
| [Demo.Outbox.Api](Demo.Outbox.Api) | 5105 | 分布式/Outbox | Transactional Outbox |
| [Demo.Inbox.Api](Demo.Inbox.Api) | 5106 | 分布式/Inbox | Transactional Inbox |
| [Demo.MqConsumer.Api](Demo.MqConsumer.Api) | 5107 | 分布式/消息消费 | ACK / 重试 / DLQ |
| [Demo.Saga.Api](Demo.Saga.Api) | 5108 | 分布式/Saga | 编排式补偿 |
| [Demo.RateLimit.Api](Demo.RateLimit.Api) | 5109 | 弹性/限流 | 固定 / 滑动窗口 |
| [Demo.Retry.Api](Demo.Retry.Api) | 5110 | 弹性/重试 | 指数退避 |
| [Demo.CircuitBreaker.Api](Demo.CircuitBreaker.Api) | 5111 | 弹性/熔断 | 开/半开/关 |
| [Demo.ShortUrl.Api](Demo.ShortUrl.Api) | 5112 | 缓存/短网址 | 短链跳转 |
| [Demo.Redis.Api](Demo.Redis.Api) | 5113 | 缓存/Redis缓存 | Cache-Aside（内存模拟） |
| [Demo.CacheProblems.Api](Demo.CacheProblems.Api) | 5114 | 缓存/缓存三大问题 | 穿透 / 击穿 / 雪崩 |
| [Demo.Seckill.Api](Demo.Seckill.Api) | 5115 | 缓存/秒杀 | 预扣 + 异步建单 |
| [Demo.PasswordAuth.Api](Demo.PasswordAuth.Api) | 5116 | 认证/密码认证 | PBKDF2 加盐哈希 |
| [Demo.JwtAuth.Api](Demo.JwtAuth.Api) | 5117 | 认证/JWT | Access + Refresh |
| [Demo.QrLogin.Api](Demo.QrLogin.Api) | 5118 | 认证/扫码登录 | Ticket 状态机 Mock |
| [Demo.Sso.Api](Demo.Sso.Api) | 5119 | 认证/单点登录 | 简化 CAS Ticket |
| [Demo.OptimisticLock.Api](Demo.OptimisticLock.Api) | 5120 | 存储/乐观锁 | 版本号 CAS |
| [Demo.DistributedLock.Api](Demo.DistributedLock.Api) | 5121 | 存储/分布式锁 | SET NX EX（内存） |
| [Demo.OrderTimeout.Api](Demo.OrderTimeout.Api) | 5122 | 支付/超时关单 | 关单 vs 支付竞态 |
| [Demo.Payment.Api](Demo.Payment.Api) | 5123 | 支付相关 | HMAC 回调幂等入账 |
| [Demo.DiLifetime.Api](Demo.DiLifetime.Api) | 5124 | CSharp/DI生命周期 | 三种生命周期对比 |
| [Demo.AsyncAwait.Console](Demo.AsyncAwait.Console) | — | CSharp/async与await | 并发与取消 |
| [Demo.Linq.Console](Demo.Linq.Console) | — | CSharp/LINQ | 常用算子演示 |
| [Demo.Delegates.Console](Demo.Delegates.Console) | — | CSharp/委托 | 委托 / 事件 / 多播 |
| [Demo.Algorithms.Tests](Demo.Algorithms.Tests) | — | 算法/* | 双指针 / 链表 / 二叉树 |

理论向笔记（CAP/BASE、选型对比、MySQL 索引原理等）不单独建可运行服务，相关点分散在上述 Demo 与注释中。

## 运行单个项目

```bash
cd Demo.RateLimit.Api
dotnet run
```

Development 下访问 Scalar：`http://localhost:{端口}/scalar`  
健康检查：`GET /health`

## 约定

- 目标框架：`net9.0`
- 应持久化的状态：各项目独立 SQLite（若需要）或进程内内存
- 日志与错误信息：简体中文
- 项目之间零引用，便于单独拷贝学习
