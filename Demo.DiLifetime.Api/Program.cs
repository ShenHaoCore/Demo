using Demo.DiLifetime.Api.Application;
using Demo.DiLifetime.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<ISingletonDemoService, SingletonDemoService>();
builder.Services.AddScoped<IScopedDemoService, ScopedDemoService>();
builder.Services.AddTransient<ITransientDemoService, TransientDemoService>();
builder.Services.AddScoped<IDiDemoAppService, DiDemoAppService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.MapControllers();
app.Run();
