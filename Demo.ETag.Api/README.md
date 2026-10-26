# Demo.ETag.Api

对应笔记：API/ETag · 端口：5103（Development 下 API 文档：http://localhost:5103/scalar）

用文档编辑场景演示 ETag 乐观并发控制：GET 返回内容指纹，PUT 必须凭 If-Match 证明「改的还是最新版本」，冲突返回 412 并附上当前指纹。

## 核心机制
- ETag 是内容指纹：对文档的 Id / Title / Content / UpdatedAt 序列化后取 SHA256 前 16 位十六进制字符并加双引号，内容不变则指纹不变。
- GET 返回 200 并写入 `ETag` 响应头；PUT 必须携带 `If-Match`，缺失直接 400——把乐观并发校验做成协议要求而非可选项。
- 防丢失更新靠 CAS 循环：比对 If-Match 与当前 ETag 一致后，用 `ConcurrentDictionary.TryUpdate(key, updated, existing)` 提交；被并发抢先则重读重试，保证不会出现两个并发 PUT 都返回 200。
- If-Match 与当前值不一致返回 412 PreconditionFailed，响应体带 `currentETag`，客户端凭新指纹决定重试或放弃。
- `NormalizeETag` 剥掉 `W/` 弱校验前缀后按强 ETag 精确比较；预置文档 `doc-1`，内存字典模拟存储。

## 关键文件
| 文件 | 说明 |
|---|---|
| [DocumentAppService.cs](Application/DocumentAppService.cs) | ETag 计算、If-Match 比对、CAS 提交循环 |
| [DocumentsController.cs](Controllers/DocumentsController.cs) | GET/PUT 路由，412 分支与 ETag 响应头 |
| [IDocumentAppService.cs](Application/IDocumentAppService.cs) | 更新结果模型：Success / NotFound / PreconditionFailed / BadRequest |
| [DocumentDtos.cs](Dtos/DocumentDtos.cs) | 文档输出与更新输入 DTO |
| [Document.cs](Entities/Document.cs) | 文档实体 |

## 运行

```bash
cd Demo.ETag.Api
dotnet run
```

```bash
# 先取当前 ETag（看响应头）
curl -i http://localhost:5103/api/documents/doc-1
# 带 If-Match 更新成功；改用旧 ETag 重发即可复现 412
curl -i -X PUT http://localhost:5103/api/documents/doc-1 -H 'If-Match: <上一步返回的 ETag>' -H 'Content-Type: application/json' -d '{"title":"新标题","content":"新内容"}'
```

## 笔记要点
- ETag 把「读-改-写」变成可检测的 CAS：写入前必须证明读取时刻与写入时刻之间无人改过。
- 412 是并发信号不是故障，客户端拿到 currentETag 后自行决定重试或放弃。
- 内存里的 `TryUpdate` 与数据库 version 列乐观锁是同一思想的两个实现层级。
