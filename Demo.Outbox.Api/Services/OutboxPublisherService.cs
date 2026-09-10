using Demo.Outbox.Api.Application;

namespace Demo.Outbox.Api.Services;

/// <summary>Outbox 发布后台服务（基础设施）。</summary>
public sealed class OutboxPublisherService(IOutboxAppService outboxAppService, ILogger<OutboxPublisherService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox 发布后台服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pending = outboxAppService.ClaimPending(20);
                foreach (var message in pending)
                {
                    // 模拟投递到消息中间件（已 claim 为 Publishing）
                    logger.LogInformation("发布 outbox 消息 {MessageId}，订单 {OrderId}", message.Id, message.OrderId);
                    outboxAppService.MarkPublished(message.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "轮询 outbox 时发生错误");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
