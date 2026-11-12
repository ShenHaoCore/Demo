namespace Demo.OrderTimeout.Api.Services;

/// <summary>按 tick 推进哈希时间轮，驱动到期关单。</summary>
public sealed class OrderTimeoutService(
    HashedTimingWheel timingWheel,
    ILogger<OrderTimeoutService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "订单超时时间轮已启动：tick={Tick}，wheelSize={Size}（一圈 {RoundSeconds}s）",
            timingWheel.TickDuration,
            timingWheel.WheelSize,
            timingWheel.TickDuration.TotalSeconds * timingWheel.WheelSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(timingWheel.TickDuration, stoppingToken);
                timingWheel.AdvanceClock();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
