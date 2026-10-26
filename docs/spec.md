# Demo 仓库规格（Spec）

> 本规格描述仓库的目标、边界与验收口径；测试缝经确认后生效。

## Problem Statement

面试准备者读 haonotes 笔记时是纯文本输入：概念「读过」但讲不出可运行的细节。面试场景要求能随口说出机制的现象级证据——重放幂等键返回什么、熔断半开放不放行、限流超阈值的响应码是什么。需要一个把笔记主题逐个落成**可运行、可交互、可观察**的小型实现的集合，把「读过」变成「讲得出」。

## Solution

每个笔记主题一个**独立 .NET 9 项目**（`Demo.{主题}.{Api|Console|Tests}`），互不引用、无共享代码：

- Api 项目用 HTTP 接口暴露机制行为，Scalar 提供交互式文档，每个项目有独立固定端口（从 5101 连续分配）
- 外部依赖（Redis、数据库、消息队列、下游服务）全部用进程内内存模拟，零外部服务即可跑
- 根 README 按类别索引全部项目并直链各项目 README；每个项目 README 讲对应笔记、核心机制、关键文件与运行方式
- 分支管理与发版遵循 Git Flow（详见 docs/branch-management.md），tag 推送自动挂 Release

## User Stories

1. As a 面试准备者, I want 按笔记主题在索引里找到对应的可运行 Demo, so that 复习某个概念时有现成的验证载体
2. As a 面试准备者, I want 每个 Demo 一条命令（`dotnet run` / `dotnet test`）就能跑起来, so that 面试前现场演示不需要搭环境
3. As a 面试准备者, I want 通过 HTTP 响应或控制台输出直接观察机制现象（429 限流、412 并发冲突、幂等重放、DLQ 毒消息）, so that 我讲的是看到的现象而不是背的描述
4. As a 面试准备者, I want 每个项目 README 列出核心机制与关键文件, so that 能快速定位「这个机制在哪几行」
5. As a 仓库使用者, I want 克隆后所有项目同时构建通过、CI 三门禁（构建与测试 / 风格 / 依赖漏洞）兜底, so that 任意时点的 main 分支都是可跑的
6. As a 仓库使用者, I want 版本与发版可预期（Git Flow + 语义化版本）, so that 可以稳定引用某个 tag 的状态

## Implementation Decisions

- **项目形态与命名**：`Demo.{主题}.{类型}`，类型按内容取 `Api` / `Console` / `Tests`；无跨项目依赖，删掉任何一个目录其余不受影响
- **端口契约**：Api 项目从 5101 起连续分配、launchSettings 固定；与笔记中旧仓库（5066–5084）不冲突
- **外部依赖模拟策略**：模拟件暴露与真实件相同的接口形状（cache-aside、SET NX EX、重试/熔断策略、Outbox/Inbox 状态机），代码里注明「教学模拟」及其与真实实现的差距
- **API 约定**：统一 Scalar 文档与 `/health`；错误路径返回语义化状态码（401/404/409/412/429），不用 200 包错误
- **工程约束**：SDK 由 global.json 锁定；编译属性与包版本分别集中在两份 props（CPM）；代码风格由 .editorconfig + CI 门禁强制
- **分支模型**：Git Flow，`master`/`develop` 受 Rulesets 保护、PR 必过三项门禁

## Testing Decisions

- **测试缝（seam）**：每个项目的**公开边界**——Api 项目的 HTTP 接口（`.http`/curl 可观察响应）、Console 项目的运行输出、`Demo.Algorithms.Tests` 的 xUnit 断言。不测内部类、不 mock 内部协作者
- **现有先例**：`Demo.Algorithms.Tests`（双指针/链表/二叉树 8 用例）+ CI 构建与测试门禁
- **好测试的定义**：断言用户可见的现象与状态码，期望值来自规格或演算过的例子，不用与实现同法重算
- **优先级说明**：多数 Demo 的价值在「可观察」而非「被测试覆盖」；对事务、并发、状态迁移类的机制（幂等、超时关单竞态、秒杀预扣）应优先补 xUnit 验证，纯直通演示不强制

## Out of Scope

- 真实基础设施接入（真实 Redis / 数据库 / 消息队列 / OAuth 提供方）——模拟件是刻意选择
- 生产化能力：鉴权加固、持久化、水平扩展、配置中心、可观测性平台
- 前端 UI
- 与旧仓库（Demo1）或其它仓库的代码共享、抽公共库

## Further Notes

- **tracker 约定**：采用 GitHub Issues 作为工单跟踪；标签体系在首个真实特性进 Issue 时按需建立，不预造
- **GLOSSARY / ADR**：出现第一个真实的领域术语分歧或架构决策时，再落 `GLOSSARY.md` 与 `docs/adr/`（惰性创建，不预造）
- 本 spec 的测试缝为初选（HTTP/控制台输出/xUnit 三类公开边界），经确认后生效
- 笔记对应关系见根 README
