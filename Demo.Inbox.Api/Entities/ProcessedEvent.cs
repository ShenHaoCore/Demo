namespace Demo.Inbox.Api.Entities;

public enum InboxStatus
{
    Processing,
    Completed
}

/// <summary>已处理（或处理中）的 Inbox 事件实体。</summary>
public record ProcessedEvent(string MessageId, string? Payload, DateTimeOffset ProcessedAt, InboxStatus Status);
