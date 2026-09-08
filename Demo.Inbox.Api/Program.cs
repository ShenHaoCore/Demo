using System.Collections.Concurrent;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<InboxStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.Inbox.Api" }));

app.MapPost("/api/events", (EventRequest request, InboxStore store, bool? fail) =>
{
    if (string.IsNullOrWhiteSpace(request.MessageId))
    {
        return Results.BadRequest(new { message = "messageId 不能为空" });
    }

    var messageId = request.MessageId.Trim();

    // 已处理过：直接返回 200，不重复执行副作用
    if (store.IsProcessed(messageId))
    {
        store.IncrementDedup();
        return Results.Ok(new
        {
            message = "消息已处理过（inbox 去重）",
            messageId,
            duplicated = true,
            stats = store.GetStats()
        });
    }

    // 模拟消费失败以便演示重投：不写入 inbox，下次可再次投递
    if (fail == true)
    {
        return Results.Json(new
        {
            message = "模拟处理失败，未写入 inbox，可重新投递",
            messageId,
            failed = true
        }, statusCode: StatusCodes.Status500InternalServerError);
    }

    var processed = store.TryProcess(messageId, request.Payload, out var alreadyProcessed);
    if (alreadyProcessed)
    {
        store.IncrementDedup();
        return Results.Ok(new
        {
            message = "消息已处理过（inbox 去重）",
            messageId,
            duplicated = true,
            stats = store.GetStats()
        });
    }

    if (!processed)
    {
        return Results.Conflict(new { message = "消息正在处理中，请稍后重试", messageId });
    }

    return Results.Ok(new
    {
        message = "事件已处理，副作用已执行（计数 +1）",
        messageId,
        duplicated = false,
        stats = store.GetStats()
    });
})
.WithName("ReceiveEvent")
.WithSummary("接收事件（按 messageId 去重）；?fail=true 模拟失败");

app.MapGet("/api/stats", (InboxStore store) => Results.Ok(store.GetStats()))
.WithName("GetStats")
.WithSummary("查看处理次数与去重次数");

app.Run();

record EventRequest(string MessageId, string? Payload);

enum InboxStatus
{
    Processing,
    Completed
}

/// <summary>
/// 内存 Inbox：用 Processing 占位模拟「去重记录与副作用同事务」。
/// 副作用失败会移除占位，允许重投；成功后才标记 Completed。
/// </summary>
sealed class InboxStore
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

record ProcessedEvent(string MessageId, string? Payload, DateTimeOffset ProcessedAt, InboxStatus Status);
