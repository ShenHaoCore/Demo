using System.Collections.Concurrent;
using Demo.Inbox.Api.Entities;

namespace Demo.Inbox.Api.Application;

/// <summary>
/// 内存 Inbox：用 Processing 占位模拟「去重记录与副作用同事务」。
/// 副作用失败会移除占位，允许重投；成功后才标记 Completed。
/// </summary>
public sealed class InboxAppService : IInboxAppService
{
    private readonly ConcurrentDictionary<string, ProcessedEvent> _inbox = new(StringComparer.Ordinal);
    private long _processedCount;
    private long _dedupCount;
    private long _sideEffectCounter;

    public bool IsProcessed(string messageId) =>
        _inbox.TryGetValue(messageId, out var entry) && entry.Status == InboxStatus.Completed;

    public bool TryProcess(string messageId, string? payload, out bool alreadyProcessed)
    {
        alreadyProcessed = false;

        if (_inbox.TryGetValue(messageId, out var existing))
        {
            if (existing.Status == InboxStatus.Completed)
            {
                alreadyProcessed = true;
                return false;
            }

            // Processing：另一请求正在处理
            return false;
        }

        var processing = new ProcessedEvent(messageId, payload, DateTimeOffset.UtcNow, InboxStatus.Processing);
        if (!_inbox.TryAdd(messageId, processing))
        {
            if (_inbox.TryGetValue(messageId, out existing) && existing.Status == InboxStatus.Completed)
            {
                alreadyProcessed = true;
            }

            return false;
        }

        try
        {
            // 副作用：仅首次处理时累加（失败则回滚 inbox 占位）
            Interlocked.Increment(ref _sideEffectCounter);
            Interlocked.Increment(ref _processedCount);
            _inbox[messageId] = processing with { Status = InboxStatus.Completed };
            return true;
        }
        catch
        {
            _inbox.TryRemove(messageId, out _);
            throw;
        }
    }

    public void IncrementDedup() => Interlocked.Increment(ref _dedupCount);

    public object GetStats() => new
    {
        processedCount = Interlocked.Read(ref _processedCount),
        dedupCount = Interlocked.Read(ref _dedupCount),
        sideEffectCounter = Interlocked.Read(ref _sideEffectCounter),
        inboxSize = _inbox.Count,
        note = "Processing 占位失败会移除，模拟与副作用同事务回滚"
    };
}
