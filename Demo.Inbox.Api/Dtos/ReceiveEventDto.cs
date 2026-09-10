namespace Demo.Inbox.Api.Dtos;

/// <summary>接收事件输入。</summary>
public record ReceiveEventDto(string MessageId, string? Payload);
