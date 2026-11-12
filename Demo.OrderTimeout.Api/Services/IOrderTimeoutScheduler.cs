namespace Demo.OrderTimeout.Api.Services;

/// <summary>订单超时调度：创建时挂入，支付/关单时取消。</summary>
public interface IOrderTimeoutScheduler
{
    string ProviderName { get; }

    void Schedule(Guid orderId, TimeSpan delay, Action onTimeout);

    void Cancel(Guid orderId);
}
