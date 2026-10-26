# Demo.Payment.Api

对应笔记：**支付** · 端口：`5123`（Development 下 API 文档：http://localhost:5123/scalar）

模拟支付网关回调入账：`HMAC-SHA256` 验签 + `notifyId` 幂等，演示"同一笔回调重发多次，只入账一次"以及各失败分支的状态码选择。

## 核心机制

- 回调 `POST /api/payments/notify` **先验签后业务**：payload 为 `paymentId|notifyId|amount|status`（amount 用 InvariantCulture 文本），密钥来自配置 `Payment:NotifySecret`（默认 `demo-payment-secret`）；签名不合法返回 `401`，`status` 非 `success` 的合法回调直接 `200` 忽略入账。
- 签名比较用 `CryptographicOperations.FixedTimeEquals`（固定时间，防时序侧信道）；非法 hex、长度不等直接判失败。
- **幂等入账**：`notifyId` 先在 `_notifyIndex` 中 `TryAdd` 占位，占不到即视为重发的同一回调，返回 `Duplicate`（`200` + `alreadyPaid`，而非 `409`——重发通知要"应答成功"，网关才会停止重试）；占到之后再对支付单做 `Pending -> Paid` 的 CAS `TryUpdate`。
- **错账窗口**（代码注释原话）：若先 CAS 入账、后查重，会出现"已 Paid 却因其他回调先到而返回 Duplicate"的错序，所以占位必须发生在入账之前。
- 失败分支会回滚占位：支付单不存在（`404`）、回调金额与支付单不一致（`409 Conflict`）、已被其他 `notifyId` 入账（`200`，带 `lastNotifyId`）——此时同一 `notifyId` 修正后可重试。
- `GET /api/payments/notify/sign-help` 给出签名算法、payload 格式与示例，便于手工构造合法回调。

## 关键文件

| 文件 | 说明 |
|---|---|
| [PaymentsController.cs](Controllers/PaymentsController.cs) | 创建/查询支付单、回调验签与五种 `NotifyKind` 分支 |
| [PaymentAppService.cs](Application/PaymentAppService.cs) | `notifyId` 占位 + `Pending -> Paid` CAS 入账 |
| [PaymentSecurity.cs](Services/PaymentSecurity.cs) | HMAC-SHA256 计算与固定时间 hex 比较 |
| [PaymentRecord.cs](Entities/PaymentRecord.cs) | `record PaymentRecord`（含 `LastNotifyId`）与 `PaymentStatus` |

## 运行

```bash
cd Demo.Payment.Api
dotnet run
```

```bash
# 创建支付单，取回 id
PAY_ID=$(curl -s -X POST http://localhost:5123/api/payments \
  -H "Content-Type: application/json" \
  -d '{"orderId":"o-1001","amount":10.5}' | jq -r .id)

# 本地计算 HMAC 签名（payload: paymentId|notifyId|amount|status）
SIG=$(printf %s "$PAY_ID|n-001|10.5|success" | \
  openssl dgst -sha256 -hmac "demo-payment-secret" | awk '{print $NF}')

# 回调入账；重发同一 notifyId 观察幂等响应
curl -s -X POST http://localhost:5123/api/payments/notify \
  -H "Content-Type: application/json" \
  -d "{\"paymentId\":\"$PAY_ID\",\"notifyId\":\"n-001\",\"amount\":10.5,\"status\":\"success\",\"signature\":\"$SIG\"}"
```

## 笔记要点

- 幂等键占位必须先于入账 CAS，否则存在"已 Paid 却回 Duplicate"的错账窗口。
- 重复回调答 `200 alreadyPaid` 而非 `409`：幂等响应要对重发方"友好"，避免网关无限重试。
- 验签先行且用固定时间比较；签名失败（401）与非成功状态（200 忽略）是两个不同分支。
