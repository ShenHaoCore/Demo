using System.Collections.Concurrent;
using System.Threading.Channels;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<SeckillStore>();
builder.Services.AddHostedService<OrderBuilderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.Seckill.Api" }));

app.MapPost("/api/seckill/activities", (CreateActivityRequest request, SeckillStore store) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Name) || request.Stock <= 0)
    {
        return Results.BadRequest(new { message = "请提供 name 与 stock（>0）" });
    }

    var activity = store.CreateActivity(request.Name.Trim(), request.Stock);
    return Results.Created($"/api/seckill/activities/{activity.Id}", new
    {
        id = activity.Id,
        name = activity.Name,
        stock = activity.Stock,
        remaining = activity.Remaining,
        createdAt = activity.CreatedAt,
        message = "秒杀活动已创建"
    });
})
.WithName("CreateSeckillActivity")
.WithSummary("创建秒杀活动与库存");

app.MapGet("/api/seckill/activities/{id:guid}", (Guid id, SeckillStore store) =>
{
    if (!store.TryGetActivity(id, out var activity))
    {
        return Results.NotFound(new { message = "活动不存在", id });
    }

    return Results.Ok(new
    {
        id = activity.Id,
        name = activity.Name,
        stock = activity.Stock,
        remaining = Volatile.Read(ref activity.Remaining),
        successCount = activity.SuccessCount,
        createdAt = activity.CreatedAt
    });
})
.WithName("GetSeckillActivity")
.WithSummary("查询活动状态与剩余库存");

app.MapPost("/api/seckill/{id:guid}/grab", (Guid id, GrabRequest? request, SeckillStore store) =>
{
    var userId = string.IsNullOrWhiteSpace(request?.UserId)
        ? Guid.NewGuid().ToString("N")[..8]
        : request!.UserId.Trim();

    var result = store.TryGrab(id, userId);
    return result.Status switch
    {
        GrabStatus.NotFound => Results.NotFound(new { message = "活动不存在", id }),
        GrabStatus.SoldOut => Results.Conflict(new
        {
            message = "库存不足，抢购失败（超卖防护）",
            activityId = id,
            userId,
            remaining = result.Remaining
        }),
        GrabStatus.Success => Results.Ok(new
        {
            message = "预扣库存成功，已进入建单队列",
            activityId = id,
            userId,
            ticketId = result.TicketId,
            remaining = result.Remaining,
            orderStatus = "Queued"
        }),
        _ => Results.StatusCode(500)
    };
})
.WithName("GrabSeckill")
.WithSummary("预扣库存（原子），成功进内存队列");

app.MapGet("/api/seckill/orders/{ticketId:guid}", (Guid ticketId, SeckillStore store) =>
{
    if (!store.TryGetOrder(ticketId, out var order))
    {
        return Results.NotFound(new { message = "订单/票据不存在", ticketId });
    }

    return Results.Ok(new
    {
        ticketId = order.TicketId,
        orderId = order.OrderId,
        activityId = order.ActivityId,
        userId = order.UserId,
        status = order.Status.ToString(),
        createdAt = order.CreatedAt,
        builtAt = order.BuiltAt
    });
})
.WithName("GetSeckillOrder")
.WithSummary("查询抢购票据/订单状态");

app.Run();

sealed class SeckillStore
{
    private readonly ConcurrentDictionary<Guid, Activity> _activities = new();
    private readonly ConcurrentDictionary<Guid, OrderTicket> _orders = new();
    private readonly Channel<OrderTicket> _queue = Channel.CreateUnbounded<OrderTicket>();

    public ChannelReader<OrderTicket> Reader => _queue.Reader;

    public Activity CreateActivity(string name, int stock)
    {
        var activity = new Activity(Guid.NewGuid(), name, stock, stock, 0, DateTimeOffset.UtcNow);
        _activities[activity.Id] = activity;
        return activity;
    }

    public bool TryGetActivity(Guid id, out Activity activity) =>
        _activities.TryGetValue(id, out activity!);

    public GrabResult TryGrab(Guid activityId, string userId)
    {
        if (!_activities.TryGetValue(activityId, out var activity))
        {
            return new GrabResult(GrabStatus.NotFound, null, 0);
        }

        // 原子预扣：Interlocked 保证不超卖
        // 本 Demo 聚焦预扣+异步建单；「一人一单」未实现，生产需按 activityId+userId 去重
        while (true)
        {
            var current = Volatile.Read(ref activity.Remaining);
            if (current <= 0)
            {
                return new GrabResult(GrabStatus.SoldOut, null, 0);
            }

            if (Interlocked.CompareExchange(ref activity.Remaining, current - 1, current) == current)
            {
                Interlocked.Increment(ref activity.SuccessCount);
                var ticket = new OrderTicket(
                    TicketId: Guid.NewGuid(),
                    OrderId: null,
                    ActivityId: activityId,
                    UserId: userId,
                    Status: OrderStatus.Queued,
                    CreatedAt: DateTimeOffset.UtcNow,
                    BuiltAt: null);

                _orders[ticket.TicketId] = ticket;
                _queue.Writer.TryWrite(ticket);
                return new GrabResult(GrabStatus.Success, ticket.TicketId, current - 1);
            }
        }
    }

    public bool TryGetOrder(Guid ticketId, out OrderTicket order) =>
        _orders.TryGetValue(ticketId, out order!);

    public void MarkBuilt(Guid ticketId, Guid orderId)
    {
        _orders.AddOrUpdate(
            ticketId,
            _ => throw new InvalidOperationException("票据不存在"),
            (_, existing) => existing with
            {
                OrderId = orderId,
                Status = OrderStatus.Built,
                BuiltAt = DateTimeOffset.UtcNow
            });
    }
}

sealed class OrderBuilderService(SeckillStore store) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var ticket in store.Reader.ReadAllAsync(stoppingToken))
        {
            // 模拟异步建单耗时
            await Task.Delay(30, stoppingToken);
            store.MarkBuilt(ticket.TicketId, Guid.NewGuid());
        }
    }
}

enum GrabStatus
{
    Success,
    SoldOut,
    NotFound
}

enum OrderStatus
{
    Queued,
    Built
}

sealed class Activity(Guid id, string name, int stock, int remaining, int successCount, DateTimeOffset createdAt)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public int Stock { get; } = stock;
    public int Remaining = remaining;
    public int SuccessCount = successCount;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}

record CreateActivityRequest(string Name, int Stock);

record GrabRequest(string? UserId);

record GrabResult(GrabStatus Status, Guid? TicketId, int Remaining);

record OrderTicket(
    Guid TicketId,
    Guid? OrderId,
    Guid ActivityId,
    string UserId,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? BuiltAt);
