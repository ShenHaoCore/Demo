using System.Collections.Concurrent;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddHostedService<OrderTimeoutService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.OrderTimeout.Api" }));

app.MapPost("/api/orders", (CreateOrderRequest request, OrderStore store) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Product) || request.Amount <= 0)
    {
        return Results.BadRequest(new { message = "请提供 product 与 amount（>0）" });
    }

    // 默认可调短超时便于演示（秒）
    var timeoutSeconds = request.TimeoutSeconds is > 0 ? request.TimeoutSeconds.Value : 15;
    timeoutSeconds = Math.Clamp(timeoutSeconds, 3, 3600);

    var order = store.Create(request.Product.Trim(), request.Amount, TimeSpan.FromSeconds(timeoutSeconds));
    return Results.Created($"/api/orders/{order.Id}", new
    {
        id = order.Id,
        product = order.Product,
        amount = order.Amount,
        status = order.Status.ToString(),
        createdAt = order.CreatedAt,
        expireAt = order.ExpireAt,
        timeoutSeconds,
        message = "待支付订单已创建，超时将自动关单"
    });
})
.WithName("CreateOrder")
.WithSummary("创建待支付订单（可调 timeoutSeconds）");

app.MapGet("/api/orders/{id:guid}", (Guid id, OrderStore store) =>
{
    if (!store.TryGet(id, out var order))
    {
        return Results.NotFound(new { message = "订单不存在", id });
    }

    return Results.Ok(ToDto(order));
})
.WithName("GetOrder")
.WithSummary("查询订单状态");

app.MapPost("/api/orders/{id:guid}/pay", (Guid id, OrderStore store) =>
{
    var result = store.TryPay(id);
    return result.Status switch
    {
        TransitionStatus.NotFound => Results.NotFound(new { message = "订单不存在", id }),
        TransitionStatus.Conflict => Results.Conflict(new
        {
            message = "支付失败：订单已非待支付状态（可能已被超时关单）",
            id,
            currentStatus = result.Order?.Status.ToString()
        }),
        TransitionStatus.Success => Results.Ok(new
        {
            message = "支付成功（CAS：Pending -> Paid）",
            order = ToDto(result.Order!)
        }),
        _ => Results.StatusCode(500)
    };
})
.WithName("PayOrder")
.WithSummary("支付订单；与关单用状态 CAS 竞态");

app.MapPost("/api/orders/{id:guid}/close", (Guid id, OrderStore store) =>
{
    var result = store.TryClose(id, "手动关单");
    return result.Status switch
    {
        TransitionStatus.NotFound => Results.NotFound(new { message = "订单不存在", id }),
        TransitionStatus.Conflict => Results.Conflict(new
        {
            message = "关单失败：订单已非待支付状态（可能已支付）",
            id,
            currentStatus = result.Order?.Status.ToString()
        }),
        TransitionStatus.Success => Results.Ok(new
        {
            message = "关单成功（CAS：Pending -> Closed）",
            order = ToDto(result.Order!)
        }),
        _ => Results.StatusCode(500)
    };
})
.WithName("CloseOrder")
.WithSummary("手动关单（同样使用 CAS）");

app.Run();

static object ToDto(Order order) => new
{
    id = order.Id,
    product = order.Product,
    amount = order.Amount,
    status = order.Status.ToString(),
    createdAt = order.CreatedAt,
    expireAt = order.ExpireAt,
    paidAt = order.PaidAt,
    closedAt = order.ClosedAt,
    closeReason = order.CloseReason
};

sealed class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Order Create(string product, decimal amount, TimeSpan timeout)
    {
        var now = DateTimeOffset.UtcNow;
        var order = new Order(
            Id: Guid.NewGuid(),
            Product: product,
            Amount: amount,
            Status: OrderStatus.Pending,
            CreatedAt: now,
            ExpireAt: now.Add(timeout),
            PaidAt: null,
            ClosedAt: null,
            CloseReason: null);

        _orders[order.Id] = order;
        return order;
    }

    public bool TryGet(Guid id, out Order order) =>
        _orders.TryGetValue(id, out order!);

    public IEnumerable<Order> Snapshot() => _orders.Values.ToArray();

    public TransitionResult TryPay(Guid id)
    {
        while (true)
        {
            if (!_orders.TryGetValue(id, out var current))
            {
                return new TransitionResult(TransitionStatus.NotFound, null);
            }

            if (current.Status != OrderStatus.Pending)
            {
                return new TransitionResult(TransitionStatus.Conflict, current);
            }

            var updated = current with
            {
                Status = OrderStatus.Paid,
                PaidAt = DateTimeOffset.UtcNow
            };

            if (_orders.TryUpdate(id, updated, current))
            {
                return new TransitionResult(TransitionStatus.Success, updated);
            }
        }
    }

    public TransitionResult TryClose(Guid id, string reason)
    {
        while (true)
        {
            if (!_orders.TryGetValue(id, out var current))
            {
                return new TransitionResult(TransitionStatus.NotFound, null);
            }

            if (current.Status != OrderStatus.Pending)
            {
                return new TransitionResult(TransitionStatus.Conflict, current);
            }

            var updated = current with
            {
                Status = OrderStatus.Closed,
                ClosedAt = DateTimeOffset.UtcNow,
                CloseReason = reason
            };

            if (_orders.TryUpdate(id, updated, current))
            {
                return new TransitionResult(TransitionStatus.Success, updated);
            }
        }
    }
}

sealed class OrderTimeoutService(OrderStore store, ILogger<OrderTimeoutService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var order in store.Snapshot())
            {
                if (order.Status == OrderStatus.Pending && order.ExpireAt <= now)
                {
                    var result = store.TryClose(order.Id, "超时自动关单");
                    if (result.Status == TransitionStatus.Success)
                    {
                        logger.LogInformation("订单 {OrderId} 已超时关单", order.Id);
                    }
                    else if (result.Status == TransitionStatus.Conflict)
                    {
                        logger.LogInformation("订单 {OrderId} 关单与支付竞态：当前状态 {Status}",
                            order.Id, result.Order?.Status);
                    }
                }
            }

            await Task.Delay(500, stoppingToken);
        }
    }
}

enum OrderStatus
{
    Pending,
    Paid,
    Closed
}

enum TransitionStatus
{
    Success,
    Conflict,
    NotFound
}

record Order(
    Guid Id,
    string Product,
    decimal Amount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpireAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ClosedAt,
    string? CloseReason);

record CreateOrderRequest(string Product, decimal Amount, int? TimeoutSeconds);

record TransitionResult(TransitionStatus Status, Order? Order);
