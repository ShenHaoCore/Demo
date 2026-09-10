using Demo.MqConsumer.Api.Entities;

namespace Demo.MqConsumer.Api.Application;

/// <summary>消息 Broker 应用服务契约。</summary>
public interface IMessageAppService
{
    /// <summary>含首次在内的最大尝试次数（即最多重试 MaxAttempts-1 次）。</summary>
    const int DefaultMaxAttempts = 3;

    BrokerMessage Enqueue(string payload, int failUntilAttempt);

    bool TryDequeue(out BrokerMessage? message);

    void AckSuccess(Guid id);

    void AckFailure(Guid id, string error);

    object GetQueueSnapshot();

    IReadOnlyList<BrokerMessage> GetDlqSnapshot();
}
