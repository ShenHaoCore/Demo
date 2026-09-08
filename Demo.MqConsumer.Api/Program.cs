using System.Collections.Concurrent;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<InMemoryBroker>();
builder.Services.AddHostedService<MessageConsumerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.MqConsumer.Api" }));

app.MapPost("/api/messages", (PublishMessageRequest request, InMemoryBroker broker) =>
{
    if (string.IsNullOrWhiteSpace(request.Payload))
    {
        return Results.BadRequest(new { message = "payload 不能为空" });
    }

    var queued = broker.Enqueue(request.Payload.Trim(), request.FailUntilAttempt ?? 0);
    return Results.Accepted($"/api/queue", new
    {
        info = "消息已投递到内存队列",
        message = queued
    });
})
.WithName("PublishMessage")
.WithSummary("投递消息到内存 Broker（failUntilAttempt 可模拟前 N 次处理失败）");

app.MapGet("/api/queue", (InMemoryBroker broker) => Results.Ok(broker.GetQueueSnapshot()))
.WithName("GetQueue")
.WithSummary("查看队列中的消息");

app.MapGet("/api/dlq", (InMemoryBroker broker) => Results.Ok(broker.GetDlqSnapshot()))
.WithName("GetDlq")
.WithSummary("查看死信队列");

app.Run();

record PublishMessageRequest(string Payload, int? FailUntilAttempt);

enum MessageStatus
{
    Queued,
    Processing,
    Succeeded,
    DeadLetter
}

record BrokerMessage(
    Guid Id,
    string Payload,
    MessageStatus Status,
    int AttemptCount,
    int FailUntilAttempt,
    int MaxRetries,
    DateTimeOffset EnqueuedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? CompletedAt,
    string? LastError);

sealed class InMemoryBroker
{
    public const int DefaultMaxRetries = 3;

    private readonly object _gate = new();
    private readonly ConcurrentQueue<Guid> _ready = new();
    private readonly ConcurrentDictionary<Guid, BrokerMessage> _messages = new();
    private readonly ConcurrentDictionary<Guid, BrokerMessage> _dlq = new();

    public BrokerMessage Enqueue(string payload, int failUntilAttempt)
    {
        var message = new BrokerMessage(
            Guid.NewGuid(),
            payload,
            MessageStatus.Queued,
            AttemptCount: 0,
            FailUntilAttempt: Math.Max(0, failUntilAttempt),
            MaxRetries: DefaultMaxRetries,
            DateTimeOffset.UtcNow,
            null,
            null,
            null);

        _messages[message.Id] = message;
        _ready.Enqueue(message.Id);
        return message;
    }

    public bool TryDequeue(out BrokerMessage? message)
    {
        message = null;
        lock (_gate)
        {
            while (_ready.TryDequeue(out var id))
            {
                if (!_messages.TryGetValue(id, out var current))
                {
                    continue;
                }

                if (current.Status is MessageStatus.Succeeded or MessageStatus.DeadLetter)
                {
                    continue;
                }

                var processing = current with
                {
                    Status = MessageStatus.Processing,
                    AttemptCount = current.AttemptCount + 1,
                    LastAttemptAt = DateTimeOffset.UtcNow
                };
                _messages[id] = processing;
                message = processing;
                return true;
            }
        }

        return false;
    }

    public void AckSuccess(Guid id)
    {
        lock (_gate)
        {
            if (!_messages.TryGetValue(id, out var current))
            {
                return;
            }

            _messages[id] = current with
            {
                Status = MessageStatus.Succeeded,
                CompletedAt = DateTimeOffset.UtcNow,
                LastError = null
            };
        }
    }

    public void AckFailure(Guid id, string error)
    {
        lock (_gate)
        {
            if (!_messages.TryGetValue(id, out var current))
            {
                return;
            }

            if (current.AttemptCount >= current.MaxRetries)
            {
                var dead = current with
                {
                    Status = MessageStatus.DeadLetter,
                    CompletedAt = DateTimeOffset.UtcNow,
                    LastError = error
                };
                _messages[id] = dead;
                _dlq[id] = dead;
                return;
            }

            var requeued = current with
            {
                Status = MessageStatus.Queued,
                LastError = error
            };
            _messages[id] = requeued;
            _ready.Enqueue(id);
        }
    }

    public object GetQueueSnapshot()
    {
        var items = _messages.Values
            .Where(m => m.Status is MessageStatus.Queued or MessageStatus.Processing)
            .OrderBy(m => m.EnqueuedAt)
            .ToList();

        var succeeded = _messages.Values
            .Where(m => m.Status == MessageStatus.Succeeded)
            .OrderByDescending(m => m.CompletedAt)
            .ToList();

        return new { readyOrProcessing = items, succeeded, maxRetries = DefaultMaxRetries };
    }

    public IReadOnlyList<BrokerMessage> GetDlqSnapshot() =>
        _dlq.Values.OrderByDescending(m => m.CompletedAt).ToList();
}

sealed class MessageConsumerService(InMemoryBroker broker, ILogger<MessageConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("内存 MQ 消费者已启动（最大重试 {MaxRetries} 次）", InMemoryBroker.DefaultMaxRetries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!broker.TryDequeue(out var message) || message is null)
                {
                    await Task.Delay(300, stoppingToken);
                    continue;
                }

                // 手动 ACK 语义：处理成功 AckSuccess，失败 AckFailure（超限进 DLQ）
                if (message.AttemptCount <= message.FailUntilAttempt)
                {
                    var error = $"模拟处理失败（第 {message.AttemptCount} 次，failUntilAttempt={message.FailUntilAttempt}）";
                    logger.LogWarning("消息 {Id} {Error}", message.Id, error);
                    broker.AckFailure(message.Id, error);
                }
                else
                {
                    logger.LogInformation("消息 {Id} 处理成功（第 {Attempt} 次）", message.Id, message.AttemptCount);
                    broker.AckSuccess(message.Id);
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
