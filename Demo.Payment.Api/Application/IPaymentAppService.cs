using Demo.Payment.Api.Dtos;
using Demo.Payment.Api.Entities;

namespace Demo.Payment.Api.Application;

/// <summary>支付应用服务契约。</summary>
public interface IPaymentAppService
{
    PaymentRecord Create(string orderId, decimal amount);

    bool TryGet(Guid id, out PaymentRecord payment);

    NotifyResult TryApplyNotify(Guid paymentId, string notifyId, decimal amount);
}
