using Demo.OrderTimeout.Api.Application;
using Demo.OrderTimeout.Api.Dtos;
using Demo.OrderTimeout.Api.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Demo.OrderTimeout.Api.Services;

/// <summary>轮询 Redis ZSET：到期订单 Claim 后 CAS 关单。</summary>
public sealed class RedisZSetOrderTimeoutService(
    RedisZSetOrderTimeoutScheduler scheduler,
    IOrderAppService orderAppService,
    IOptions<OrderTimeoutOptions> options,
    ILogger<RedisZSetOrderTimeoutService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(
            Math.Clamp(options.Value.PollIntervalMs, 100, 60_000));

        logger.LogInformation(
            "订单超时 Redis ZSET 已启动：key={Key}，poll={Interval}ms",
            scheduler.Key,
            interval.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DrainExpired();
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Redis ZSET 轮询失败，将重试");
                await Task.Delay(interval, stoppingToken);
            }
        }
    }

    private void DrainExpired()
    {
        foreach (var orderId in scheduler.ClaimExpired())
        {
            var result = orderAppService.TryClose(orderId, "超时自动关单");
            if (result.Status == TransitionStatus.Success)
            {
                logger.LogInformation("订单 {OrderId} 已超时关单（Redis ZSET）", orderId);
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
}
