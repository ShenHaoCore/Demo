# Demo.Versioning.Api

对应笔记：API/API版本化 · 端口：5102（Development 下 API 文档：http://localhost:5102/scalar）

用 Asp.Versioning 演示同一产品 API 的 V1/V2 共存：版本可从 URL 段、Query 或 Header 读取，版本差异体现在响应字段上，OpenAPI 文档也按版本拆成两份。

## 核心机制
- 一个路由模板 `api/v{version:apiVersion}/products` 同时支持三种版本读法：URL 段、Query `?api-version=`、Header `api-version`，由 `ApiVersionReader.Combine` 组合声明。
- 版本差异是行为差异：V2 响应带 `description` 字段，V1 忽略客户端传入的 description（服务端按空串存储）；两个 Controller 共享同一应用服务，只差一个 `IncludeDescription` 常量。
- 未指定版本时默认 1.0（`AssumeDefaultVersionWhenUnspecified`），并回写 `api-supported-versions` 响应头（`ReportApiVersions`）告知可用版本。
- v1 / v2 各注册一份 OpenAPI 文档：`AddOpenApi(documentName)` 配合 `ShouldInclude` 按 GroupName 过滤端点，Scalar 页面上按文档切换。
- 版本号、文档名、参数名集中在 `ApiVersions.cs` 常量中；V1/V2 控制器分居 `Controllers/V1/`、`Controllers/V2/` 命名空间。

## 关键文件
| 文件 | 说明 |
|---|---|
| [ApiVersions.cs](ApiVersions.cs) | 版本号、文档名、`api-version` 参数名的唯一来源 |
| [ServiceCollectionExtensions.cs](Extensions/ServiceCollectionExtensions.cs) | 版本读取配置与分版本 OpenAPI 文档注册 |
| [WebApplicationExtensions.cs](Extensions/WebApplicationExtensions.cs) | 映射 OpenAPI 并把两份文档挂到 Scalar |
| [ProductController.cs](Controllers/V2/ProductController.cs) | V2 控制器；V1 位于 Controllers/V1/ 下同构 |
| [ProductAppService.cs](Application/ProductAppService.cs) | `includeDescription` 参数决定 V1/V2 输出差异 |

## 运行

```bash
cd Demo.Versioning.Api
dotnet run
```

```bash
# URL 段指定版本
curl http://localhost:5102/api/v1/products
# Query 指定版本（响应含 description 字段）
curl 'http://localhost:5102/api/products?api-version=2.0'
# Header 指定版本
curl http://localhost:5102/api/products -H 'api-version: 1.0'
```

## 笔记要点
- 三种版本读法各有取舍：URL 段直观、便于缓存与审计，Header/Query 不污染资源路径，可按团队约定选择或并用。
- 版本化不是复制两套业务代码，而是同一服务按版本参数分流行为。
- OpenAPI 文档必须随版本拆分，否则文档与运行时行为对不上。
