using System.Collections.Concurrent;
using System.Threading.Channels;
using Demo.Seckill.Api.Dtos;
using Demo.Seckill.Api.Entities;

namespace Demo.Seckill.Api.Application;

/// <summary>秒杀应用服务。</summary>
public sealed class SeckillAppService : ISeckillAppService
{
    private readonly ConcurrentDictionary<Guid, Activity> _activities = new();
    private readonly ConcurrentDictionary<Guid, OrderTicket> _orders = new();
    private readonly Channel<OrderTicket> _queue = Channel.CreateUnbounded<OrderTicket>();

    public ChannelReader<OrderTicket> Reader => _queue.Reader;

    public Activity CreateActivity(string name, int stock)
    {
        var activity = new Activity(Guid.NewGuid(), name, stock, stock, 0, DateTimeOffset.UtcNow);
        _activities[activity.Id] = activity;
        return activity;
    }

    public bool TryGetActivity(Guid id, out Activity activity) =>
        _activities.TryGetValue(id, out activity!);

    public GrabResult TryGrab(Guid activityId, string userId)
    {
        if (!_activities.TryGetValue(activityId, out var activity))
        {
            return new GrabResult(GrabStatus.NotFound, null, 0);
        }

        // 原子预扣：Interlocked 保证不超卖
        // 本 Demo 聚焦预扣+异步建单；「一人一单」未实现，生产需按 activityId+userId 去重
        while (true)
        {
            var current = Volatile.Read(ref activity.Remaining);
            if (current <= 0)
            {
                return new GrabResult(GrabStatus.SoldOut, null, 0);
            }

            if (Interlocked.CompareExchange(ref activity.Remaining, current - 1, current) == current)
            {
                Interlocked.Increment(ref activity.SuccessCount);
                var ticket = new OrderTicket(
                    TicketId: Guid.NewGuid(),
                    OrderId: null,
                    ActivityId: activityId,
                    UserId: userId,
                    Status: OrderStatus.Queued,
                    CreatedAt: DateTimeOffset.UtcNow,
                    BuiltAt: null);

                _orders[ticket.TicketId] = ticket;
                _queue.Writer.TryWrite(ticket);
                return new GrabResult(GrabStatus.Success, ticket.TicketId, current - 1);
            }
        }
    }

    public bool TryGetOrder(Guid ticketId, out OrderTicket order) =>
        _orders.TryGetValue(ticketId, out order!);

    public void MarkBuilt(Guid ticketId, Guid orderId)
    {
        _orders.AddOrUpdate(
            ticketId,
            _ => throw new InvalidOperationException("票据不存在"),
            (_, existing) => existing with
            {
                OrderId = orderId,
                Status = OrderStatus.Built,
                BuiltAt = DateTimeOffset.UtcNow
            });
    }
}
