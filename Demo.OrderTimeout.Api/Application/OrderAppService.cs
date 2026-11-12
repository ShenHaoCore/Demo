using System.Collections.Concurrent;
using Demo.OrderTimeout.Api.Dtos;
using Demo.OrderTimeout.Api.Entities;
using Demo.OrderTimeout.Api.Services;

namespace Demo.OrderTimeout.Api.Application;

/// <summary>订单应用服务：创建时挂超时调度，到期 CAS 关单；支付时取消调度。</summary>
public sealed class OrderAppService(
    IOrderTimeoutScheduler scheduler,
    ILogger<OrderAppService> logger) : IOrderAppService
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public string SchedulerProvider => scheduler.ProviderName;

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

        var orderId = order.Id;
        scheduler.Schedule(orderId, timeout, () => OnTimeout(orderId));

        logger.LogInformation(
            "订单 {OrderId} 已挂入 {Provider}，{TimeoutMinutes:0.#} 分钟后未支付将关单",
            orderId,
            scheduler.ProviderName,
            timeout.TotalMinutes);

        return order;
    }

    public bool TryGet(Guid id, out Order order) =>
        _orders.TryGetValue(id, out order!);

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
                scheduler.Cancel(id);
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
                scheduler.Cancel(id);
                return new TransitionResult(TransitionStatus.Success, updated);
            }
        }
    }

    private void OnTimeout(Guid orderId)
    {
        var result = TryClose(orderId, "超时自动关单");
        if (result.Status == TransitionStatus.Success)
        {
            logger.LogInformation(
                "订单 {OrderId} 已超时关单（{Provider}）",
                orderId,
                scheduler.ProviderName);
        }
        else if (result.Status == TransitionStatus.Conflict)
        {
            logger.LogInformation(
                "订单 {OrderId} 关单与支付竞态：当前状态 {Status}",
                orderId,
                result.Order?.Status);
        }
    }
}
