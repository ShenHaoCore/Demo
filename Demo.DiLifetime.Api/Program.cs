using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ISingletonDemoService, SingletonDemoService>();
builder.Services.AddScoped<IScopedDemoService, ScopedDemoService>();
builder.Services.AddTransient<ITransientDemoService, TransientDemoService>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/health", () => Results.Ok(new { status = "健康", service = "Demo.DiLifetime.Api" }));

app.MapGet("/api/di/demo", (HttpContext http) =>
{
    // 同一请求内解析两次，对比三种生命周期
    var sp = http.RequestServices;

    var singleton1 = sp.GetRequiredService<ISingletonDemoService>();
    var scoped1 = sp.GetRequiredService<IScopedDemoService>();
    var transient1 = sp.GetRequiredService<ITransientDemoService>();

    var singleton2 = sp.GetRequiredService<ISingletonDemoService>();
    var scoped2 = sp.GetRequiredService<IScopedDemoService>();
    var transient2 = sp.GetRequiredService<ITransientDemoService>();

    return Results.Ok(new
    {
        message = "依赖注入生命周期演示（同一请求内 Resolve 两次）",
        requestId = http.TraceIdentifier,
        firstResolve = new
        {
            singleton = singleton1.Id,
            scoped = scoped1.Id,
            transient = transient1.Id
        },
        secondResolve = new
        {
            singleton = singleton2.Id,
            scoped = scoped2.Id,
            transient = transient2.Id
        },
        explanation = new
        {
            transient = "Transient：每次 Resolve 都新建实例，故同一请求内两次 Id 不同。",
            scoped = "Scoped：同一请求（同一 scope）共用一个实例，故两次 Id 相同；新请求会换新 Id。",
            singleton = "Singleton：整个应用进程共用一个实例，故跨请求 Id 也保持相同。"
        }
    });
});

app.Run();

interface IHasId
{
    Guid Id { get; }
}

interface ISingletonDemoService : IHasId;
interface IScopedDemoService : IHasId;
interface ITransientDemoService : IHasId;

sealed class SingletonDemoService : ISingletonDemoService
{
    public Guid Id { get; } = Guid.NewGuid();
}

sealed class ScopedDemoService : IScopedDemoService
{
    public Guid Id { get; } = Guid.NewGuid();
}

sealed class TransientDemoService : ITransientDemoService
{
    public Guid Id { get; } = Guid.NewGuid();
}
