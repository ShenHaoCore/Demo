# Demo.I18n.Api

对应笔记：**CSharp / 国际化** · 端口：`5125`（Development 下 API 文档：http://localhost:5125/scalar）

`RequestLocalization` + `IStringLocalizer` 的最小可用组合：支持 `zh-CN` / `en`，演示 resx 消息本地化、业务数据的按文化翻译，以及 culture 解析优先级的验证方法。

## 核心机制

- `UseRequestLocalization` 挂两个 Provider，顺序是刻意安排的教学点：`QueryStringRequestCultureProvider`（`?culture=en`）优先于 `AcceptLanguageHeaderRequestCultureProvider`；支持文化 `zh-CN`/`en`，默认 `zh-CN`，不支持的文化回落默认值。
- resx 本地化：`IStringLocalizer<SharedResources>`，中性 `SharedResources.resx` 放中文兜底，`SharedResources.en.resx` 放英文，共 `Welcome`、`SupportedCultures`、`ProductNotFound` 三个键。
- 业务数据翻译走另一条路：`Product.Translations` 字典按文化键存名称/描述，`ProductAppService` 按当前 `CurrentUICulture` 解析——框架只管 resx，数据翻译的回退要自己写。
- **翻译回退链**：精确匹配（`zh-CN`）→ 父文化（`en-US` → `en`）→ 固定兜底 `zh-CN` → 任一翻译；DTO 回带实际命中的 `culture` 字段。
- 错误消息也本地化：`GET /api/products/999` 的 404 `message` 随当前文化变化。
- `GET /api/culture` 回显当前 Culture / UICulture 与命中的 Provider 类型名，是验证优先级的最快入口。

## 关键文件

| 文件 | 说明 |
|---|---|
| [Program.cs](Program.cs) | `AddLocalization`、支持文化清单与 Provider 顺序 |
| [ProductAppService.cs](Application/ProductAppService.cs) | 商品翻译解析与四级回退链 |
| [CultureController.cs](Controllers/CultureController.cs) | 回显当前请求文化与命中的 Provider |
| [ProductsController.cs](Controllers/ProductsController.cs) | 商品列表/详情，404 消息走 `IStringLocalizer` |
| [SharedResources.cs](Resources/SharedResources.cs) | 本地化标记类（配套 `.resx` 与 `.en.resx`） |

## 运行

```bash
cd Demo.I18n.Api
dotnet run
```

```bash
# QueryString 优先级最高
curl -s "http://localhost:5125/api/messages/welcome?culture=en"

# Accept-Language 兜底；en-US 命中父文化 en
curl -s "http://localhost:5125/api/products/1" -H "Accept-Language: en-US"

# 默认 zh-CN；查看命中的 Provider
curl -s http://localhost:5125/api/culture
```

## 笔记要点

- resx（框架消息）与业务数据翻译是两套机制：前者 `IStringLocalizer` 现成可用，后者的回退链（精确 → 父文化 → 兜底）要自己实现。
- 教学场景把 `?culture=` 放在 `Accept-Language` 之前便于手工切换；生产上通常由 Cookie 或网关决定。
