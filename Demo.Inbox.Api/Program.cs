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

sealed class InboxStore
{
    private readonly ConcurrentDictionary<string, ProcessedEvent> _inbox = new(StringComparer.Ordinal);
    private long _processedCount;
    private long _dedupCount;
    private long _sideEffectCounter;

    public bool IsProcessed(string messageId) => _inbox.ContainsKey(messageId);

    public bool TryProcess(string messageId, string? payload, out bool alreadyProcessed)
    {
        alreadyProcessed = false;

        var entry = new ProcessedEvent(messageId, payload, DateTimeOffset.UtcNow);
        if (!_inbox.TryAdd(messageId, entry))
        {
            alreadyProcessed = true;
            return false;
        }

        // 副作用：仅首次处理时累加
        Interlocked.Increment(ref _sideEffectCounter);
        Interlocked.Increment(ref _processedCount);
        return true;
    }

    public void IncrementDedup() => Interlocked.Increment(ref _dedupCount);

    public object GetStats() => new
    {
        processedCount = Interlocked.Read(ref _processedCount),
        dedupCount = Interlocked.Read(ref _dedupCount),
        sideEffectCounter = Interlocked.Read(ref _sideEffectCounter),
        inboxSize = _inbox.Count
    };
}

record ProcessedEvent(string MessageId, string? Payload, DateTimeOffset ProcessedAt);
