# Demo.DiLifetime.Api

对应笔记：**CSharp / DI 生命周期** · 端口：`5124`（Development 下 API 文档：http://localhost:5124/scalar）

三种生命周期（Singleton / Scoped / Transient）各注册一个带 `Guid Id` 的服务，`GET /api/di/demo` 在**同一请求内 Resolve 两次**，用两组 Id 的异同直观展示每种生命周期的实例数。

## 核心机制

- `SingletonDemoService` / `ScopedDemoService` / `TransientDemoService` 只有一个只读 `Guid Id`，构造时生成，是观察实例身份的唯一依据。
- **Singleton**：两次 Resolve 的 Id 相同；换一个请求再来，Id 依旧相同（整个进程一个实例）。
- **Scoped**：同一请求（同一个 scope）内两次 Resolve 的 Id 相同；换请求 Id 变——ASP.NET Core 中请求即 scope。
- **Transient**：同一请求内两次 Resolve 的 Id 也不同，每次 `GetRequiredService` 都新建。
- `DiDemoAppService` 自身注册为 Scoped，但通过注入的 `IServiceProvider` 在方法内现场解析，而不是把三个服务注入构造函数——只有这样才能在"一次调用里"对比两次 Resolve。
- Id 的变化规律正是排查 captive dependency（Scoped 被单例捕获变成事实单例）的手段：被捕获后其 Id 会像 Singleton 一样跨请求不变。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Program.cs](Program.cs) | 三种生命周期的注册方式 |
| [DiDemoAppService.cs](Application/DiDemoAppService.cs) | 同一请求内 Resolve 两次并组装对比结果与解释 |
| [DiDemoServices.cs](Services/DiDemoServices.cs) | 三个演示服务：`IHasId` 接口与 `Guid Id` 实现 |
| [DiDemoController.cs](Controllers/DiDemoController.cs) | `GET /api/di/demo` 端点 |

## 运行

```bash
cd Demo.DiLifetime.Api
dotnet run
```

```bash
# 多发几次，对比两次 Resolve 的 Id 与跨请求的变化
curl -s http://localhost:5124/api/di/demo | jq
```

## 笔记要点

- 判断生命周期只看一个问题："实例的 Id 何时改变"——从不 / 每请求 / 每 Resolve。
- Transient 与 Scoped 的差异只有在同一 scope 内多次 Resolve 才可见；"构造函数注入即只解析一次"的场景里两者等价。
