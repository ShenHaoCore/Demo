using System.Collections.Concurrent;
using System.Text.Json;
using Demo.Outbox.Api.Entities;

namespace Demo.Outbox.Api.Application;

/// <summary>Outbox / 订单应用服务。</summary>
public sealed class OutboxAppService : IOutboxAppService
{
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();

    public Task<(Order Order, OutboxMessage Message)> CreateOrderWithOutboxAsync(string product, int quantity)
    {
        lock (_gate)
        {
            // 内存事务语义：订单与 outbox 一起成功，任一步失败则都不落库
            var orderId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var order = new Order(orderId, product, quantity, now);
            var payload = JsonSerializer.Serialize(new { orderId, product, quantity });
            var message = new OutboxMessage(
                messageId,
                orderId,
                "OrderCreated",
                payload,
                OutboxStatus.Pending,
                now,
                null);

            if (!_orders.TryAdd(order.Id, order))
            {
                throw new InvalidOperationException("写入订单失败，事务已回滚");
            }

            if (!_messages.TryAdd(message.Id, message))
            {
                _orders.TryRemove(order.Id, out _);
                throw new InvalidOperationException("写入 outbox 失败，事务已回滚");
            }

            return Task.FromResult((order, message));
        }
    }

    public Task<IReadOnlyList<OutboxMessage>> GetMessagesAsync() =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>(
            _messages.Values.OrderBy(m => m.CreatedAt).ToList());

    public Task<IReadOnlyList<Order>> GetOrdersAsync() =>
        Task.FromResult<IReadOnlyList<Order>>(
            _orders.Values.OrderByDescending(o => o.CreatedAt).ToList());

    /// <inheritdoc />
    public IReadOnlyList<OutboxMessage> ClaimPending(int take)
    {
        lock (_gate)
        {
            var claimed = new List<OutboxMessage>();
            foreach (var message in _messages.Values
                         .Where(m => m.Status == OutboxStatus.Pending)
                         .OrderBy(m => m.CreatedAt)
                         .Take(take))
            {
                var publishing = message with { Status = OutboxStatus.Publishing };
                _messages[message.Id] = publishing;
                claimed.Add(publishing);
            }

            return claimed;
        }
    }

    public bool MarkPublished(Guid id)
    {
        lock (_gate)
        {
            if (!_messages.TryGetValue(id, out var current) || current.Status != OutboxStatus.Publishing)
            {
                return false;
            }

            _messages[id] = current with
            {
                Status = OutboxStatus.Published,
                PublishedAt = DateTimeOffset.UtcNow
            };
            return true;
        }
    }
}
