namespace Demo.OrderTimeout.Api.Options;

/// <summary>超时关单调度配置。</summary>
public sealed class OrderTimeoutOptions
{
    public const string SectionName = "OrderTimeout";

    /// <summary>TimingWheel | Redis | RabbitMq。</summary>
    public string Provider { get; set; } = "TimingWheel";

    public string Redis { get; set; } = "localhost:6379";

    /// <summary>ZSET key。</summary>
    public string RedisKey { get; set; } = "order:timeout";

    /// <summary>Redis 轮询间隔（毫秒）。</summary>
    public int PollIntervalMs { get; set; } = 500;

    /// <summary>amqp://user:pass@host:5672/</summary>
    public string RabbitMq { get; set; } = "amqp://guest:guest@localhost:5672/";

    public string DelayExchange { get; set; } = "order.timeout.delay";

    public string DelayQueue { get; set; } = "order.timeout.delay";

    public string DelayRoutingKey { get; set; } = "delay";

    public string CloseExchange { get; set; } = "order.timeout.close";

    public string CloseQueue { get; set; } = "order.timeout.close";

    public string CloseRoutingKey { get; set; } = "close";
}
