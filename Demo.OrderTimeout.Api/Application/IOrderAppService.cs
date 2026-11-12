using Demo.OrderTimeout.Api.Dtos;
using Demo.OrderTimeout.Api.Entities;

namespace Demo.OrderTimeout.Api.Application;

/// <summary>订单应用服务契约。</summary>
public interface IOrderAppService
{
    string SchedulerProvider { get; }

    Order Create(string product, decimal amount, TimeSpan timeout);

    bool TryGet(Guid id, out Order order);

    TransitionResult TryPay(Guid id);

    TransitionResult TryClose(Guid id, string reason);
}
