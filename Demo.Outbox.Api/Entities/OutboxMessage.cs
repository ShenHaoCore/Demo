namespace Demo.Outbox.Api.Entities;

public enum OutboxStatus
{
    Pending,
    Publishing,
    Published
}

/// <summary>Outbox 消息实体。</summary>
public record OutboxMessage(
    Guid Id,
    Guid OrderId,
    string Type,
    string Payload,
    OutboxStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);
