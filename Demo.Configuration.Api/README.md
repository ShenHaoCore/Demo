# Demo.Configuration.Api

对应笔记：**CSharp / 配置** · 端口：`5126`（Development 下 API 文档：http://localhost:5126/scalar）

演示配置的**加载顺序、来源追踪与启动校验**：同一份 `Demo` 节配置在多环境 / 环境变量覆盖下的最终值，以及每个键到底来自哪个提供程序。

## 核心机制

- `WebApplication.CreateBuilder` 默认加载顺序（后覆盖前）：`appsettings.json` → `appsettings.{Environment}.json` → User Secrets（仅 Development）→ 环境变量（`Demo__ApiKey`）→ 命令行（`--Demo:ApiKey=xxx`）。
- `launchSettings` 配了四个 Profile：`Development` / `Staging` / `Production` / `Development_EnvOverride`，最后一个用环境变量覆盖 `Demo__ApiKey` 与 `Demo__MaxRequestsPerMinute=42`，现场观察覆盖效果。
- Options 模式绑定 `Demo` 节为 `DemoOptions`，并用 `ValidateOnStart` + `IValidateOptions`（`DisplayName`/`ApiKey` 非空、`MaxRequestsPerMinute > 0`）做**启动即校验**：坏配置让进程直接起不来，而不是带着默认值悄悄运行。
- `GET /api/config` 返回快照：环境名、IOptions 绑定结果（`ApiKey` 掩码显示）+ `ValueSources`——按与框架一致的规则（从后往前找第一个含该键的 Provider）标出每个键的最终来源。
- `GET /api/config/providers` 列出提供程序注册顺序，对应"同名键后面的覆盖前面的"。
- 仓库内 `ApiKey` 全部是 `DEMO-ONLY-*` 假值；真实密钥应走环境变量 / User Secrets / 密钥保管服务，切勿提交仓库。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Program.cs](Program.cs) | 加载顺序注释、Options 绑定与 `ValidateOnStart` |
| [ConfigAppService.cs](Application/ConfigAppService.cs) | 配置快照、`ValueSources` 来源解析、ApiKey 掩码 |
| [DemoOptions.cs](Options/DemoOptions.cs) | `Demo` 节的强类型 Options 模型 |
| [DemoOptionsValidator.cs](Options/DemoOptionsValidator.cs) | 启动校验规则（非空 / 大于 0） |
| [launchSettings.json](Properties/launchSettings.json) | 四个 Profile（含环境变量覆盖演示） |

## 运行

```bash
cd Demo.Configuration.Api
dotnet run --launch-profile Development_EnvOverride
```

```bash
# 观察 environment=Development、ApiKey 来自环境变量提供程序、MaxRequestsPerMinute=42
curl -s http://localhost:5126/api/config

# 查看提供程序顺序与覆盖关系
curl -s http://localhost:5126/api/config/providers
```

## 笔记要点

- "谁覆盖谁"不必背：`/api/config` 的 `ValueSources` 就是解析规则的自证（从后往前找第一个命中的 Provider）。
- `ValidateOnStart` 把配置错误从"第一次用到时才炸"左移到启动时刻，是没有配置中心时最便宜的防线。
