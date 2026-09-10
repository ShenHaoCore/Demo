namespace Demo.MqConsumer.Api.Dtos;

/// <summary>投递消息输入。</summary>
public record PublishMessageDto(string Payload, int? FailUntilAttempt);
