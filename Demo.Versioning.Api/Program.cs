using Asp.Versioning;
using Asp.Versioning.Builder;
using Scalar.AspNetCore;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("api-version"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var products = new ConcurrentDictionary<Guid, ProductRecord>();
products[Guid.Parse("11111111-1111-1111-1111-111111111111")] = new ProductRecord(
    Guid.Parse("11111111-1111-1111-1111-111111111111"),
    "笔记本",
    5999m,
    "轻薄办公本，适合日常开发");
products[Guid.Parse("22222222-2222-2222-2222-222222222222")] = new ProductRecord(
    Guid.Parse("22222222-2222-2222-2222-222222222222"),
    "机械键盘",
    399m,
    "青轴，可热插拔");

ApiVersionSet versionSet = app.NewApiVersionSet("Products")
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// URL 版本：/api/v1/products
app.MapGet("/api/v{version:apiVersion}/products", () =>
{
    var list = products.Values
        .Select(p => new ProductV1(p.Id, p.Name, p.Price))
        .OrderBy(p => p.Name)
        .ToList();
    return Results.Ok(list);
})
.WithApiVersionSet(versionSet)
.MapToApiVersion(1.0);

// URL 版本：/api/v2/products（多 description）
app.MapGet("/api/v{version:apiVersion}/products", () =>
{
    var list = products.Values
        .Select(p => new ProductV2(p.Id, p.Name, p.Price, p.Description))
        .OrderBy(p => p.Name)
        .ToList();
    return Results.Ok(list);
})
.WithApiVersionSet(versionSet)
.MapToApiVersion(2.0);

// Header 版本：api-version: 1.0 / 2.0，路径 /api/products
app.MapGet("/api/products", (HttpContext context) =>
{
    var version = context.GetRequestedApiVersion()?.MajorVersion ?? 1;
    if (version >= 2)
    {
        return Results.Ok(products.Values
            .Select(p => new ProductV2(p.Id, p.Name, p.Price, p.Description))
            .OrderBy(p => p.Name)
            .ToList());
    }

    return Results.Ok(products.Values
        .Select(p => new ProductV1(p.Id, p.Name, p.Price))
        .OrderBy(p => p.Name)
        .ToList());
})
.WithApiVersionSet(versionSet)
.MapToApiVersion(1.0)
.MapToApiVersion(2.0);

app.Run();

record ProductRecord(Guid Id, string Name, decimal Price, string Description);
record ProductV1(Guid Id, string Name, decimal Price);
record ProductV2(Guid Id, string Name, decimal Price, string Description);
