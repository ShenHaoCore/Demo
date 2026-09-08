using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Scalar.AspNetCore;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IdempotencyStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.Idempotent.Api" }));

app.MapPost("/api/orders", async (HttpRequest request, IdempotencyStore store) =>
{
    if (!request.Headers.TryGetValue("Idempotency-Key", out var keyValues) ||
        string.IsNullOrWhiteSpace(keyValues.ToString()))
    {
        return Results.BadRequest(new { message = "缺少必填请求头 Idempotency-Key" });
    }

    var key = keyValues.ToString().Trim();
    using var reader = new StreamReader(request.Body);
    var bodyText = await reader.ReadToEndAsync();
    var bodyHash = ComputeHash(bodyText);

    CreateOrderRequest? orderRequest;
    try
    {
        orderRequest = string.IsNullOrWhiteSpace(bodyText)
            ? null
            : JsonSerializer.Deserialize<CreateOrderRequest>(bodyText, jsonOptions);
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { message = "请求体 JSON 无效" });
    }

    if (orderRequest is null || string.IsNullOrWhiteSpace(orderRequest.Product) || orderRequest.Quantity <= 0)
    {
        return Results.BadRequest(new { message = "请提供有效的 product 与 quantity（>0）" });
    }

    var result = store.GetOrCreate(key, bodyHash, () =>
    {
        var order = new OrderResponse(
            Id: Guid.NewGuid(),
            Product: orderRequest.Product.Trim(),
            Quantity: orderRequest.Quantity,
            CreatedAt: DateTimeOffset.UtcNow,
            IdempotencyKey: key);

        return new IdempotencySnapshot(bodyHash, StatusCodes.Status201Created, order);
    });

    return result switch
    {
        IdempotencyOutcome.Conflict => Results.Conflict(new
        {
            message = "同一 Idempotency-Key 已用于不同请求体，拒绝处理",
            idempotencyKey = key
        }),
        IdempotencyOutcome.Ok(var snapshot) =>
            Results.Json(snapshot.Body, statusCode: snapshot.StatusCode, options: jsonOptions),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
})
.WithName("CreateOrder")
.WithSummary("创建订单（要求 Idempotency-Key，重复请求返回首次结果）");

app.Run();

static string ComputeHash(string content)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content ?? string.Empty));
    return Convert.ToHexString(bytes);
}

sealed class IdempotencyStore
{
    // Demo 简化：无 TTL / 无淘汰。生产应对 Idempotency-Key 设过期并定期清理 _store/_keyLocks。
    private readonly ConcurrentDictionary<string, IdempotencySnapshot> _store = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, object> _keyLocks = new(StringComparer.Ordinal);

    public IdempotencyOutcome GetOrCreate(string key, string bodyHash, Func<IdempotencySnapshot> factory)
    {
        if (_store.TryGetValue(key, out var existing))
        {
            return SameBody(existing, bodyHash);
        }

        var gate = _keyLocks.GetOrAdd(key, static _ => new object());
        lock (gate)
        {
            if (_store.TryGetValue(key, out existing))
            {
                return SameBody(existing, bodyHash);
            }

            var snapshot = factory();
            _store[key] = snapshot;
            return new IdempotencyOutcome.Ok(snapshot);
        }
    }

    private static IdempotencyOutcome SameBody(IdempotencySnapshot existing, string bodyHash) =>
        string.Equals(existing.BodyHash, bodyHash, StringComparison.Ordinal)
            ? new IdempotencyOutcome.Ok(existing)
            : IdempotencyOutcome.Conflict.Instance;
}

abstract record IdempotencyOutcome
{
    public sealed record Ok(IdempotencySnapshot Snapshot) : IdempotencyOutcome;
    public sealed record Conflict : IdempotencyOutcome
    {
        public static readonly Conflict Instance = new();
    }
}

record CreateOrderRequest(string Product, int Quantity);

record OrderResponse(Guid Id, string Product, int Quantity, DateTimeOffset CreatedAt, string IdempotencyKey);

record IdempotencySnapshot(string BodyHash, int StatusCode, object Body);
