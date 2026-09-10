using System.Threading.Channels;
using Demo.Seckill.Api.Dtos;
using Demo.Seckill.Api.Entities;

namespace Demo.Seckill.Api.Application;

/// <summary>秒杀应用服务契约。</summary>
public interface ISeckillAppService
{
    ChannelReader<OrderTicket> Reader { get; }

    Activity CreateActivity(string name, int stock);

    bool TryGetActivity(Guid id, out Activity activity);

    GrabResult TryGrab(Guid activityId, string userId);

    bool TryGetOrder(Guid ticketId, out OrderTicket order);

    void MarkBuilt(Guid ticketId, Guid orderId);
}
