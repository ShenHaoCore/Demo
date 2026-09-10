using System.Collections.Concurrent;
using Demo.MqConsumer.Api.Entities;

namespace Demo.MqConsumer.Api.Application;

/// <summary>内存消息 Broker 应用服务。</summary>
public sealed class MessageAppService : IMessageAppService
{
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
            MaxAttempts: IMessageAppService.DefaultMaxAttempts,
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

            // AttemptCount 已含本次失败；达到 MaxAttempts 则进 DLQ（不再重试）
            if (current.AttemptCount >= current.MaxAttempts)
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

        return new
        {
            readyOrProcessing = items,
            succeeded,
            maxAttempts = IMessageAppService.DefaultMaxAttempts,
            maxRetries = IMessageAppService.DefaultMaxAttempts - 1,
            note = $"最多尝试 {IMessageAppService.DefaultMaxAttempts} 次（含首次，即最多重试 {IMessageAppService.DefaultMaxAttempts - 1} 次）"
        };
    }

    public IReadOnlyList<BrokerMessage> GetDlqSnapshot() =>
        _dlq.Values.OrderByDescending(m => m.CompletedAt).ToList();
}
