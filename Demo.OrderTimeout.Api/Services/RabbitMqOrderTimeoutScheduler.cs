using System.Text;
using Demo.OrderTimeout.Api.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Demo.OrderTimeout.Api.Services;

/// <summary>
/// RabbitMQ 延迟关单：消息写入延迟队列并设 Expiration（TTL），
/// 到期后经死信交换机（DLX）路由到关单队列，由消费者 CAS 关单。
/// shortcut: 无法从队列中途撤回消息，Cancel 为空操作，依赖关单 CAS；
/// 单消息 TTL 仅在队头到期（队头阻塞），演示流量小可忽略，生产可换延迟插件或分片队列。
/// </summary>
public sealed class RabbitMqOrderTimeoutScheduler : IOrderTimeoutScheduler, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly OrderTimeoutOptions _options;
    private readonly ILogger<RabbitMqOrderTimeoutScheduler> _logger;

    public RabbitMqOrderTimeoutScheduler(
        IOptions<OrderTimeoutOptions> options,
        ILogger<RabbitMqOrderTimeoutScheduler> logger)
    {
        _options = options.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_options.RabbitMq),
            DispatchConsumersAsync = false
        };
        _connection = factory.CreateConnection("order-timeout");
        _channel = _connection.CreateModel();
        DeclareTopology();
    }

    public string ProviderName => "RabbitMq";

    public IConnection Connection => _connection;

    public OrderTimeoutOptions Options => _options;

    public void Schedule(Guid orderId, TimeSpan delay, Action onTimeout)
    {
        // 到期回调由死信队列消费者触发，本机 Action 不使用。
        _ = onTimeout;

        var ms = Math.Max(1, (long)Math.Ceiling(delay.TotalMilliseconds));
        if (ms > int.MaxValue)
        {
            ms = int.MaxValue;
        }

        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "text/plain";
        props.Expiration = ms.ToString();
        props.MessageId = orderId.ToString("D");

        var body = Encoding.UTF8.GetBytes(orderId.ToString("D"));
        _channel.BasicPublish(
            exchange: _options.DelayExchange,
            routingKey: _options.DelayRoutingKey,
            basicProperties: props,
            body: body);

        _logger.LogDebug("订单 {OrderId} 已投递延迟队列，TTL={TtlMs}ms", orderId, ms);
    }

    public void Cancel(Guid orderId)
    {
        // RabbitMQ 无法按业务键删除队列中间消息；支付成功后死信仍可能到达，靠 TryClose CAS 忽略。
        _ = orderId;
    }

    private void DeclareTopology()
    {
        _channel.ExchangeDeclare(_options.CloseExchange, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(
            queue: _options.CloseQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        _channel.QueueBind(
            queue: _options.CloseQueue,
            exchange: _options.CloseExchange,
            routingKey: _options.CloseRoutingKey);

        var delayArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.CloseExchange,
            ["x-dead-letter-routing-key"] = _options.CloseRoutingKey
        };

        _channel.ExchangeDeclare(_options.DelayExchange, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(
            queue: _options.DelayQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: delayArgs);
        _channel.QueueBind(
            queue: _options.DelayQueue,
            exchange: _options.DelayExchange,
            routingKey: _options.DelayRoutingKey);

        _logger.LogInformation(
            "RabbitMQ 超时拓扑已声明：delay={DelayQueue} -> DLX={CloseExchange} -> close={CloseQueue}",
            _options.DelayQueue,
            _options.CloseExchange,
            _options.CloseQueue);
    }

    public void Dispose()
    {
        try
        {
            _channel.Close();
            _channel.Dispose();
        }
        catch
        {
            // ignore shutdown races
        }

        try
        {
            _connection.Close();
            _connection.Dispose();
        }
        catch
        {
            // ignore shutdown races
        }
    }
}
