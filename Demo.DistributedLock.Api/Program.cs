using Demo.DistributedLock.Api.Application;
using Demo.DistributedLock.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<InMemoryLockService>();
builder.Services.AddSingleton<ILockAppService, LockAppService>();
builder.Services.AddSingleton<ICriticalAppService, CriticalAppService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();
