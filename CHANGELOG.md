# 更新日志

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循[语义化版本](https://semver.org/lang/zh-CN/)。

发版流程见 [docs/branch-management.md](docs/branch-management.md)：`release/x.y.z` 冻结期把「未发布」段改写为版本号，并补写「升级到本版本要做什么」。

## [未发布]

### 新增
- Git Flow 分支管理：`develop` 集成主干 + `release` / `hotfix` 双合，Rulesets 保护 `master` / `develop` / `v*` tag
- 工程基建：MIT LICENSE、`global.json` 锁定 SDK feature band、`Directory.Build.props` 集中编译属性、中央包管理（`Directory.Packages.props`）、`.editorconfig`
- CI 新增门禁：风格检查（`dotnet format --verify-no-changes`）、依赖漏洞检查（`dotnet list package --vulnerable`，High/Critical 拦截）
- Dependabot 每周检查 NuGet 包与 GitHub Actions 版本
- 30 个项目补齐独立 README（对应笔记 / 核心机制 / 关键文件 / 运行 / 笔记要点）
- 修正脚手架残留的 `.http` 文件（端口与路由对齐真实代码）
