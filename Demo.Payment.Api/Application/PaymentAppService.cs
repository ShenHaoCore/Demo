using System.Collections.Concurrent;
using Demo.Payment.Api.Dtos;
using Demo.Payment.Api.Entities;

namespace Demo.Payment.Api.Application;

/// <summary>支付应用服务。</summary>
public sealed class PaymentAppService : IPaymentAppService
{
    private readonly ConcurrentDictionary<Guid, PaymentRecord> _payments = new();
    private readonly ConcurrentDictionary<string, Guid> _notifyIndex = new(StringComparer.Ordinal);

    public PaymentRecord Create(string orderId, decimal amount)
    {
        var payment = new PaymentRecord(
            Id: Guid.NewGuid(),
            OrderId: orderId,
            Amount: amount,
            Status: PaymentStatus.Pending,
            CreatedAt: DateTimeOffset.UtcNow,
            PaidAt: null,
            LastNotifyId: null);

        _payments[payment.Id] = payment;
        return payment;
    }

    public bool TryGet(Guid id, out PaymentRecord payment) =>
        _payments.TryGetValue(id, out payment!);

    public NotifyResult TryApplyNotify(Guid paymentId, string notifyId, decimal amount)
    {
        // 幂等：先占位 notifyId，再 CAS 入账，避免「已 Paid 却返回 Duplicate」的错账窗口
        if (_notifyIndex.TryGetValue(notifyId, out var existingPaymentId))
        {
            _payments.TryGetValue(existingPaymentId, out var existing);
            return new NotifyResult(NotifyKind.Duplicate, existing);
        }

        if (!_notifyIndex.TryAdd(notifyId, paymentId))
        {
            _payments.TryGetValue(_notifyIndex[notifyId], out var dup);
            return new NotifyResult(NotifyKind.Duplicate, dup);
        }

        while (true)
        {
            if (!_payments.TryGetValue(paymentId, out var current))
            {
                _notifyIndex.TryRemove(notifyId, out _);
                return new NotifyResult(NotifyKind.NotFound, null);
            }

            if (current.Amount != amount)
            {
                _notifyIndex.TryRemove(notifyId, out _);
                return new NotifyResult(NotifyKind.AmountMismatch, current);
            }

            if (current.Status == PaymentStatus.Paid)
            {
                _notifyIndex.TryRemove(notifyId, out _);
                return new NotifyResult(NotifyKind.AlreadyPaidOtherNotify, current);
            }

            var updated = current with
            {
                Status = PaymentStatus.Paid,
                PaidAt = DateTimeOffset.UtcNow,
                LastNotifyId = notifyId
            };

            if (_payments.TryUpdate(paymentId, updated, current))
            {
                return new NotifyResult(NotifyKind.Applied, updated);
            }
        }
    }
}
