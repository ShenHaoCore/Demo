using Demo.CircuitBreaker.Api.Application;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<SimpleCircuitBreaker>();
builder.Services.AddSingleton(new DownstreamOptions());
builder.Services.AddSingleton<IDownstreamAppService, DownstreamAppService>();
builder.Services.AddSingleton<ICircuitBreakerAppService, CircuitBreakerAppService>();
builder.Services.AddSingleton<ICallAppService, CallAppService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();
