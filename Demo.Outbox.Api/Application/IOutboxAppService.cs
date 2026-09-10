using Demo.Outbox.Api.Entities;

namespace Demo.Outbox.Api.Application;

/// <summary>Outbox / 订单应用服务契约。</summary>
public interface IOutboxAppService
{
    Task<(Order Order, OutboxMessage Message)> CreateOrderWithOutboxAsync(string product, int quantity);

    Task<IReadOnlyList<OutboxMessage>> GetMessagesAsync();

    Task<IReadOnlyList<Order>> GetOrdersAsync();

    /// <summary>真正 claim：Pending → Publishing，避免多消费者重复投递。</summary>
    IReadOnlyList<OutboxMessage> ClaimPending(int take);

    bool MarkPublished(Guid id);
}
