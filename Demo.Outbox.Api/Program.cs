using System.Collections.Concurrent;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<OutboxStore>();
builder.Services.AddHostedService<OutboxPublisherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.Outbox.Api" }));

app.MapPost("/api/orders", (CreateOrderRequest request, OutboxStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Product) || request.Quantity <= 0)
    {
        return Results.BadRequest(new { message = "请提供有效的 product 与 quantity（>0）" });
    }

    try
    {
        var (order, message) = store.CreateOrderWithOutbox(request.Product.Trim(), request.Quantity);
        return Results.Created($"/api/orders/{order.Id}", new
        {
            message = "订单与 outbox 消息已在同一内存事务中写入",
            order,
            outboxMessageId = message.Id
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
})
.WithName("CreateOrder")
.WithSummary("创建订单并同事务写入 outbox");

app.MapGet("/api/outbox", (OutboxStore store) =>
{
    return Results.Ok(store.GetMessages());
})
.WithName("ListOutbox")
.WithSummary("查看 outbox 消息状态");

app.MapGet("/api/orders", (OutboxStore store) => Results.Ok(store.GetOrders()))
.WithName("ListOrders");

app.Run();

record CreateOrderRequest(string Product, int Quantity);

record Order(Guid Id, string Product, int Quantity, DateTimeOffset CreatedAt);

enum OutboxStatus
{
    Pending,
    Published
}

record OutboxMessage(
    Guid Id,
    Guid OrderId,
    string Type,
    string Payload,
    OutboxStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);

sealed class OutboxStore
{
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();

    public (Order Order, OutboxMessage Message) CreateOrderWithOutbox(string product, int quantity)
    {
        lock (_gate)
        {
            // 内存事务语义：订单与 outbox 一起成功，任一步失败则都不落库
            var orderId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var order = new Order(orderId, product, quantity, now);
            var message = new OutboxMessage(
                messageId,
                orderId,
                "OrderCreated",
                $$"""{"orderId":"{{orderId}}","product":"{{product}}","quantity":{{quantity}}}""",
                OutboxStatus.Pending,
                now,
                null);

            if (!_orders.TryAdd(order.Id, order))
            {
                throw new InvalidOperationException("写入订单失败，事务已回滚");
            }

            if (!_messages.TryAdd(message.Id, message))
            {
                _orders.TryRemove(order.Id, out _);
                throw new InvalidOperationException("写入 outbox 失败，事务已回滚");
            }

            return (order, message);
        }
    }

    public IReadOnlyList<OutboxMessage> GetMessages() =>
        _messages.Values.OrderBy(m => m.CreatedAt).ToList();

    public IReadOnlyList<Order> GetOrders() =>
        _orders.Values.OrderByDescending(o => o.CreatedAt).ToList();

    public IReadOnlyList<OutboxMessage> ClaimPending(int take)
    {
        lock (_gate)
        {
            return _messages.Values
                .Where(m => m.Status == OutboxStatus.Pending)
                .OrderBy(m => m.CreatedAt)
                .Take(take)
                .ToList();
        }
    }

    public bool MarkPublished(Guid id)
    {
        lock (_gate)
        {
            if (!_messages.TryGetValue(id, out var current) || current.Status != OutboxStatus.Pending)
            {
                return false;
            }

            _messages[id] = current with
            {
                Status = OutboxStatus.Published,
                PublishedAt = DateTimeOffset.UtcNow
            };
            return true;
        }
    }
}

sealed class OutboxPublisherService(OutboxStore store, ILogger<OutboxPublisherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox 发布后台服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pending = store.ClaimPending(20);
                foreach (var message in pending)
                {
                    // 模拟投递到消息中间件
                    logger.LogInformation("发布 outbox 消息 {MessageId}，订单 {OrderId}", message.Id, message.OrderId);
                    store.MarkPublished(message.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "轮询 outbox 时发生错误");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
