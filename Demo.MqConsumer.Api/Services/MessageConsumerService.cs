using Demo.MqConsumer.Api.Application;

namespace Demo.MqConsumer.Api.Services;

/// <summary>内存 MQ 消费者后台服务（基础设施）。</summary>
public sealed class MessageConsumerService(IMessageAppService messageAppService, ILogger<MessageConsumerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "内存 MQ 消费者已启动（最多尝试 {MaxAttempts} 次，含首次）",
            IMessageAppService.DefaultMaxAttempts);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!messageAppService.TryDequeue(out var message) || message is null)
                {
                    await Task.Delay(300, stoppingToken);
                    continue;
                }

                // 手动 ACK 语义：处理成功 AckSuccess，失败 AckFailure（达 MaxAttempts 进 DLQ）
                if (message.AttemptCount <= message.FailUntilAttempt)
                {
                    var error = $"模拟处理失败（第 {message.AttemptCount}/{message.MaxAttempts} 次，failUntilAttempt={message.FailUntilAttempt}）";
                    logger.LogWarning("消息 {Id} {Error}", message.Id, error);
                    messageAppService.AckFailure(message.Id, error);
                }
                else
                {
                    logger.LogInformation("消息 {Id} 处理成功（第 {Attempt}/{Max} 次）", message.Id, message.AttemptCount, message.MaxAttempts);
                    messageAppService.AckSuccess(message.Id);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "消费循环异常");
                await Task.Delay(500, stoppingToken);
            }
        }
    }
}
