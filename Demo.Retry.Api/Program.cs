using Polly;
using Polly.Retry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<UnstableDownstream>();
builder.Services.AddSingleton<ResiliencePipeline<UnstableResult>>(CreateRetryPipeline);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/unstable", (UnstableDownstream downstream, double? rate) =>
{
    if (rate is >= 0 and <= 1)
    {
        downstream.FailureRate = rate.Value;
        app.Logger.LogInformation("已更新不稳定接口失败率：{FailureRate}", downstream.FailureRate);
    }

    var result = downstream.Invoke();
    if (!result.Success)
    {
        return Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(result);
});

app.MapPost("/api/unstable/config", (UnstableConfigRequest request, UnstableDownstream downstream) =>
{
    if (request.FailureRate is < 0 or > 1)
    {
        return Results.BadRequest(new { message = "失败率必须在 0 到 1 之间" });
    }

    downstream.FailureRate = request.FailureRate;
    app.Logger.LogInformation("通过配置接口设置失败率：{FailureRate}", downstream.FailureRate);
    return Results.Ok(new { failureRate = downstream.FailureRate, message = "失败率已更新" });
});

app.MapGet("/api/proxy", async (UnstableDownstream downstream, ResiliencePipeline<UnstableResult> pipeline) =>
{
    var attempts = 0;
    UnstableResult? lastResult = null;
    string? error = null;
    var succeeded = false;

    try
    {
        lastResult = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return ValueTask.FromResult(downstream.Invoke());
        });
        succeeded = lastResult.Success;
        if (!succeeded)
        {
            error = "达到最大重试次数后仍失败";
        }
    }
    catch (Exception ex)
    {
        error = $"重试过程异常：{ex.Message}";
        app.Logger.LogError(ex, "代理调用不稳定下游失败");
    }

    return Results.Ok(new
    {
        success = succeeded,
        attempts,
        failureRate = downstream.FailureRate,
        result = lastResult,
        error,
        message = succeeded ? "代理重试后调用成功" : "代理重试后调用失败"
    });
});

app.Run();

static ResiliencePipeline<UnstableResult> CreateRetryPipeline(IServiceProvider sp)
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("RetryProxy");
    return new ResiliencePipelineBuilder<UnstableResult>()
        .AddRetry(new RetryStrategyOptions<UnstableResult>
        {
            MaxRetryAttempts = 4,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<UnstableResult>()
                .HandleResult(r => !r.Success),
            OnRetry = args =>
            {
                logger.LogWarning(
                    "代理指数退避重试：第 {Attempt} 次失败后等待 {Delay}ms",
                    args.AttemptNumber + 1,
                    args.RetryDelay.TotalMilliseconds);
                return ValueTask.CompletedTask;
            }
        })
        .Build();
}

sealed class UnstableDownstream
{
    private int _callCount;

    public double FailureRate { get; set; } = 0.7;

    public UnstableResult Invoke()
    {
        var attempt = Interlocked.Increment(ref _callCount);
        var random = Random.Shared.NextDouble();
        if (random < FailureRate)
        {
            return new UnstableResult(false, attempt, FailureRate, "下游服务暂时不可用", DateTimeOffset.UtcNow);
        }

        return new UnstableResult(true, attempt, FailureRate, "下游服务调用成功", DateTimeOffset.UtcNow);
    }
}

record UnstableResult(bool Success, int Attempt, double FailureRate, string Message, DateTimeOffset Timestamp);
record UnstableConfigRequest(double FailureRate);
