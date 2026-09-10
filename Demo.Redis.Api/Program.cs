using Demo.Redis.Api.Application;
using Demo.Redis.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<ProductDb>();
builder.Services.AddSingleton<MemoryCacheAside>();
builder.Services.AddSingleton<IProductAppService, ProductAppService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();
