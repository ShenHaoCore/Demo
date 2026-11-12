using Demo.OrderTimeout.Api.Application;
using Demo.OrderTimeout.Api.Options;
using Demo.OrderTimeout.Api.Services;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.Configure<OrderTimeoutOptions>(
    builder.Configuration.GetSection(OrderTimeoutOptions.SectionName));

var timeoutOptions = builder.Configuration
                         .GetSection(OrderTimeoutOptions.SectionName)
                         .Get<OrderTimeoutOptions>()
                     ?? new OrderTimeoutOptions();

if (string.Equals(timeoutOptions.Provider, "Redis", StringComparison.OrdinalIgnoreCase))
{
    var mux = ConnectionMultiplexer.Connect(timeoutOptions.Redis);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    builder.Services.AddSingleton<RedisZSetOrderTimeoutScheduler>();
    builder.Services.AddSingleton<IOrderTimeoutScheduler>(
        sp => sp.GetRequiredService<RedisZSetOrderTimeoutScheduler>());
    builder.Services.AddHostedService<RedisZSetOrderTimeoutService>();
}
else if (string.Equals(timeoutOptions.Provider, "RabbitMq", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<RabbitMqOrderTimeoutScheduler>();
    builder.Services.AddSingleton<IOrderTimeoutScheduler>(
        sp => sp.GetRequiredService<RabbitMqOrderTimeoutScheduler>());
    builder.Services.AddHostedService<RabbitMqDeadLetterOrderTimeoutService>();
}
else
{
    builder.Services.AddSingleton<HashedTimingWheel>();
    builder.Services.AddSingleton<IOrderTimeoutScheduler, TimingWheelOrderTimeoutScheduler>();
    builder.Services.AddHostedService<OrderTimeoutService>();
}

builder.Services.AddSingleton<IOrderAppService, OrderAppService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithDarkMode(false));
}

app.MapControllers();
app.Run();
