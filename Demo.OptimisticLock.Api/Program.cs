using System.Collections.Concurrent;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ProductStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.OptimisticLock.Api" }));

app.MapGet("/api/products/{id:int}", (int id, ProductStore store) =>
{
    if (!store.TryGet(id, out var product))
    {
        return Results.NotFound(new { message = "商品不存在", id });
    }

    return Results.Ok(new
    {
        id = product.Id,
        name = product.Name,
        stock = product.Stock,
        version = product.Version,
        message = "返回当前 version，更新库存时请带上 expectedVersion"
    });
})
.WithName("GetProduct")
.WithSummary("查询商品库存与当前 version");

app.MapPut("/api/products/{id:int}/stock", (int id, UpdateStockRequest request, ProductStore store) =>
{
    if (request is null || request.Stock < 0)
    {
        return Results.BadRequest(new { message = "stock 必须 >= 0，并提供 expectedVersion" });
    }

    var result = store.TryUpdateStock(id, request.Stock, request.ExpectedVersion);
    return result.Status switch
    {
        UpdateStatus.NotFound => Results.NotFound(new { message = "商品不存在", id }),
        UpdateStatus.Conflict => Results.Conflict(new
        {
            message = "乐观锁冲突：expectedVersion 与当前 version 不一致",
            id,
            expectedVersion = request.ExpectedVersion,
            currentVersion = result.Product?.Version,
            currentStock = result.Product?.Stock
        }),
        UpdateStatus.Success => Results.Ok(new
        {
            message = "CAS 更新成功，version 已递增",
            id = result.Product!.Id,
            name = result.Product.Name,
            stock = result.Product.Stock,
            version = result.Product.Version
        }),
        _ => Results.StatusCode(500)
    };
})
.WithName("UpdateStockOptimistic")
.WithSummary("带 expectedVersion 的 CAS 更新库存，冲突返回 409");

app.MapPost("/api/products", (CreateProductRequest request, ProductStore store) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Name) || request.Stock < 0)
    {
        return Results.BadRequest(new { message = "请提供 name 与 stock（>=0）" });
    }

    var product = store.Create(request.Name.Trim(), request.Stock);
    return Results.Created($"/api/products/{product.Id}", new
    {
        id = product.Id,
        name = product.Name,
        stock = product.Stock,
        version = product.Version
    });
})
.WithName("CreateProduct")
.WithSummary("创建带 version 的商品");

app.Run();

sealed class ProductStore
{
    private readonly ConcurrentDictionary<int, Product> _products = new();
    private int _nextId = 3;

    public ProductStore()
    {
        _products[1] = new Product(1, "乐观锁演示商品", 100, 1);
        _products[2] = new Product(2, "另一件商品", 50, 1);
    }

    public Product Create(string name, int stock)
    {
        var id = Interlocked.Increment(ref _nextId);
        var product = new Product(id, name, stock, 1);
        _products[id] = product;
        return product;
    }

    public bool TryGet(int id, out Product product) =>
        _products.TryGetValue(id, out product!);

    public UpdateResult TryUpdateStock(int id, int newStock, long expectedVersion)
    {
        while (true)
        {
            if (!_products.TryGetValue(id, out var current))
            {
                return new UpdateResult(UpdateStatus.NotFound, null);
            }

            if (current.Version != expectedVersion)
            {
                return new UpdateResult(UpdateStatus.Conflict, current);
            }

            var updated = current with
            {
                Stock = newStock,
                Version = current.Version + 1
            };

            if (_products.TryUpdate(id, updated, current))
            {
                return new UpdateResult(UpdateStatus.Success, updated);
            }
            // 并发下字典值已变，重试读取
        }
    }
}

enum UpdateStatus
{
    Success,
    Conflict,
    NotFound
}

record Product(int Id, string Name, int Stock, long Version);

record UpdateStockRequest(int Stock, long ExpectedVersion);

record CreateProductRequest(string Name, int Stock);

record UpdateResult(UpdateStatus Status, Product? Product);
