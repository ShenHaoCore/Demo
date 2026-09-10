namespace Demo.MqConsumer.Api.Entities;

public enum MessageStatus
{
    Queued,
    Processing,
    Succeeded,
    DeadLetter
}

/// <summary>内存 Broker 消息实体。</summary>
public record BrokerMessage(
    Guid Id,
    string Payload,
    MessageStatus Status,
    int AttemptCount,
    int FailUntilAttempt,
    int MaxAttempts,
    DateTimeOffset EnqueuedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? CompletedAt,
    string? LastError);
