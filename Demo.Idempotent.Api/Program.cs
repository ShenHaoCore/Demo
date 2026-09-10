using Demo.Idempotent.Api.Application.Idempotency;
using Demo.Idempotent.Api.Application.Orders;
using Demo.Idempotent.Api.Data;
using Demo.Idempotent.Api.Options;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.Configure<IdempotencyOptions>(
    builder.Configuration.GetSection(IdempotencyOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Idempotent")
                       ?? "Data Source=idempotent.db";
builder.Services.AddDbContext<IdempotentDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
builder.Services.AddScoped<IOrderAppService, OrderAppService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdempotentDbContext>();
    // Demo 无迁移：若改了模型，请手动删除 idempotent.db 后重启
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();
