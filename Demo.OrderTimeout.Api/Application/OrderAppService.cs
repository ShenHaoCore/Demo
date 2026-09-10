using System.Collections.Concurrent;
using Demo.OrderTimeout.Api.Dtos;
using Demo.OrderTimeout.Api.Entities;

namespace Demo.OrderTimeout.Api.Application;

/// <summary>订单应用服务。</summary>
public sealed class OrderAppService : IOrderAppService
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Order Create(string product, decimal amount, TimeSpan timeout)
    {
        var now = DateTimeOffset.UtcNow;
        var order = new Order(
            Id: Guid.NewGuid(),
            Product: product,
            Amount: amount,
            Status: OrderStatus.Pending,
            CreatedAt: now,
            ExpireAt: now.Add(timeout),
            PaidAt: null,
            ClosedAt: null,
            CloseReason: null);

        _orders[order.Id] = order;
        return order;
    }

    public bool TryGet(Guid id, out Order order) =>
        _orders.TryGetValue(id, out order!);

    public IEnumerable<Order> Snapshot() => _orders.Values.ToArray();

    public TransitionResult TryPay(Guid id)
    {
        while (true)
        {
            if (!_orders.TryGetValue(id, out var current))
            {
                return new TransitionResult(TransitionStatus.NotFound, null);
            }

            if (current.Status != OrderStatus.Pending)
            {
                return new TransitionResult(TransitionStatus.Conflict, current);
            }

            var updated = current with
            {
                Status = OrderStatus.Paid,
                PaidAt = DateTimeOffset.UtcNow
            };

            if (_orders.TryUpdate(id, updated, current))
            {
                return new TransitionResult(TransitionStatus.Success, updated);
            }
        }
    }

    public TransitionResult TryClose(Guid id, string reason)
    {
        while (true)
        {
            if (!_orders.TryGetValue(id, out var current))
            {
                return new TransitionResult(TransitionStatus.NotFound, null);
            }

            if (current.Status != OrderStatus.Pending)
            {
                return new TransitionResult(TransitionStatus.Conflict, current);
            }

            var updated = current with
            {
                Status = OrderStatus.Closed,
                ClosedAt = DateTimeOffset.UtcNow,
                CloseReason = reason
            };

            if (_orders.TryUpdate(id, updated, current))
            {
                return new TransitionResult(TransitionStatus.Success, updated);
            }
        }
    }
}
