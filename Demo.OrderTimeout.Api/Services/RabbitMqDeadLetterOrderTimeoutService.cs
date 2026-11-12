using System.Text;
using Demo.OrderTimeout.Api.Application;
using Demo.OrderTimeout.Api.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Demo.OrderTimeout.Api.Services;

/// <summary>消费死信关单队列：TTL 到期消息经 DLX 到达后 CAS 关单。</summary>
public sealed class RabbitMqDeadLetterOrderTimeoutService(
    RabbitMqOrderTimeoutScheduler scheduler,
    IOrderAppService orderAppService,
    ILogger<RabbitMqDeadLetterOrderTimeoutService> logger) : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = scheduler.Options;
        _channel = scheduler.Connection.CreateModel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 32, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, args) => OnReceived(args);

        _channel.BasicConsume(
            queue: options.CloseQueue,
            autoAck: false,
            consumer: consumer);

        logger.LogInformation(
            "订单超时 RabbitMQ 死信消费已启动：queue={Queue}",
            options.CloseQueue);

        return WaitUntilStoppedAsync(stoppingToken);
    }

    private async Task WaitUntilStoppedAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            try
            {
                _channel?.Close();
                _channel?.Dispose();
            }
            catch
            {
                // ignore shutdown races
            }
        }
    }

    private void OnReceived(BasicDeliverEventArgs args)
    {
        var channel = _channel;
        if (channel is null)
        {
            return;
        }

        try
        {
            var text = Encoding.UTF8.GetString(args.Body.ToArray());
            if (!Guid.TryParse(text, out var orderId))
            {
                logger.LogWarning("死信消息非法，已丢弃：{Body}", text);
                channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            var result = orderAppService.TryClose(orderId, "超时自动关单");
            if (result.Status == TransitionStatus.Success)
            {
                logger.LogInformation("订单 {OrderId} 已超时关单（RabbitMQ DLX）", orderId);
            }
            else if (result.Status == TransitionStatus.Conflict)
            {
                logger.LogInformation(
                    "订单 {OrderId} 关单与支付竞态：当前状态 {Status}",
                    orderId,
                    result.Order?.Status);
            }
            else if (result.Status == TransitionStatus.NotFound)
            {
                logger.LogInformation("订单 {OrderId} 不存在（可能已重启），忽略死信", orderId);
            }

            channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理死信关单失败，将 requeue");
            try
            {
                channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
            catch (Exception nackEx)
            {
                logger.LogError(nackEx, "BasicNack 失败");
            }
        }
    }
}
