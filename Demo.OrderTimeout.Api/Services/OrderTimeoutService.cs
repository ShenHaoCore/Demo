using Demo.OrderTimeout.Api.Application;
using Demo.OrderTimeout.Api.Dtos;
using Demo.OrderTimeout.Api.Entities;

namespace Demo.OrderTimeout.Api.Services;

/// <summary>订单超时关单后台服务（基础设施）。</summary>
public sealed class OrderTimeoutService(IOrderAppService orderAppService, ILogger<OrderTimeoutService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var order in orderAppService.Snapshot())
            {
                if (order.Status == OrderStatus.Pending && order.ExpireAt <= now)
                {
                    var result = orderAppService.TryClose(order.Id, "超时自动关单");
                    if (result.Status == TransitionStatus.Success)
                    {
                        logger.LogInformation("订单 {OrderId} 已超时关单", order.Id);
                    }
                    else if (result.Status == TransitionStatus.Conflict)
                    {
                        logger.LogInformation("订单 {OrderId} 关单与支付竞态：当前状态 {Status}",
                            order.Id, result.Order?.Status);
                    }
                }
            }

            await Task.Delay(500, stoppingToken);
        }
    }
}
