using System.Collections.Concurrent;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var orders = new ConcurrentDictionary<Guid, Order>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/orders", () =>
{
    return Results.Ok(orders.Values.OrderBy(o => o.CreatedAt).ToList());
});

app.MapGet("/api/orders/{id:guid}", (Guid id) =>
{
    if (!orders.TryGetValue(id, out var order))
    {
        return Results.NotFound(new { message = $"未找到订单：{id}" });
    }

    return Results.Ok(order);
});

app.MapPost("/api/orders", (CreateOrderRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.CustomerName))
    {
        return Results.BadRequest(new { message = "客户名称不能为空" });
    }

    if (request.Amount <= 0)
    {
        return Results.BadRequest(new { message = "订单金额必须大于 0" });
    }

    var order = new Order
    {
        Id = Guid.NewGuid(),
        CustomerName = request.CustomerName.Trim(),
        Amount = request.Amount,
        Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status.Trim(),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    orders[order.Id] = order;
    app.Logger.LogInformation("已创建订单 {OrderId}，客户：{CustomerName}", order.Id, order.CustomerName);

    return Results.Created($"/api/orders/{order.Id}", order);
});

// PUT 直接覆盖（last-write-wins）。并发控制见 Demo.ETag.Api / Demo.OptimisticLock.Api
app.MapPut("/api/orders/{id:guid}", (Guid id, UpdateOrderRequest request) =>
{
    if (!orders.TryGetValue(id, out var existing))
    {
        return Results.NotFound(new { message = $"未找到订单：{id}" });
    }

    if (string.IsNullOrWhiteSpace(request.CustomerName))
    {
        return Results.BadRequest(new { message = "客户名称不能为空" });
    }

    if (request.Amount <= 0)
    {
        return Results.BadRequest(new { message = "订单金额必须大于 0" });
    }

    if (string.IsNullOrWhiteSpace(request.Status))
    {
        return Results.BadRequest(new { message = "订单状态不能为空" });
    }

    var updated = existing with
    {
        CustomerName = request.CustomerName.Trim(),
        Amount = request.Amount,
        Status = request.Status.Trim(),
        UpdatedAt = DateTimeOffset.UtcNow
    };

    orders[id] = updated;
    app.Logger.LogInformation("已全量更新订单 {OrderId}", id);
    return Results.Ok(updated);
});

app.MapPatch("/api/orders/{id:guid}", (Guid id, PatchOrderRequest request) =>
{
    if (!orders.TryGetValue(id, out var existing))
    {
        return Results.NotFound(new { message = $"未找到订单：{id}" });
    }

    if (request.Amount is { } amount && amount <= 0)
    {
        return Results.BadRequest(new { message = "订单金额必须大于 0" });
    }

    var updated = existing with
    {
        CustomerName = string.IsNullOrWhiteSpace(request.CustomerName)
            ? existing.CustomerName
            : request.CustomerName.Trim(),
        Amount = request.Amount ?? existing.Amount,
        Status = string.IsNullOrWhiteSpace(request.Status)
            ? existing.Status
            : request.Status.Trim(),
        UpdatedAt = DateTimeOffset.UtcNow
    };

    orders[id] = updated;
    app.Logger.LogInformation("已部分更新订单 {OrderId}", id);
    return Results.Ok(updated);
});

app.MapDelete("/api/orders/{id:guid}", (Guid id) =>
{
    if (!orders.TryRemove(id, out _))
    {
        return Results.NotFound(new { message = $"未找到订单：{id}" });
    }

    app.Logger.LogInformation("已删除订单 {OrderId}", id);
    return Results.NoContent();
});

app.Run();

record Order
{
    public Guid Id { get; init; }
    public required string CustomerName { get; init; }
    public decimal Amount { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

record CreateOrderRequest(string CustomerName, decimal Amount, string? Status);

record UpdateOrderRequest(string CustomerName, decimal Amount, string Status);

record PatchOrderRequest(string? CustomerName, decimal? Amount, string? Status);
