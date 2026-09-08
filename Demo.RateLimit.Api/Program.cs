using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfterSeconds = 1;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                message = "请求过于频繁，请稍后重试",
                retryAfterSeconds
            },
            cancellationToken);
    };

    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("sliding", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
                SegmentsPerWindow = 5,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/requests/unlimited", () =>
{
    return Results.Ok(new
    {
        message = "无限制接口调用成功",
        timestamp = DateTimeOffset.UtcNow
    });
});

app.MapGet("/api/requests/fixed", () =>
{
    return Results.Ok(new
    {
        message = "固定窗口限流接口调用成功（10 秒内最多 5 次）",
        timestamp = DateTimeOffset.UtcNow
    });
}).RequireRateLimiting("fixed");

app.MapGet("/api/requests/sliding", () =>
{
    return Results.Ok(new
    {
        message = "滑动窗口限流接口调用成功（10 秒窗口内最多 5 次）",
        timestamp = DateTimeOffset.UtcNow
    });
}).RequireRateLimiting("sliding");

app.Run();
