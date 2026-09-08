using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<SimpleCircuitBreaker>();
builder.Services.AddSingleton(new DownstreamOptions());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/downstream", (DownstreamOptions options) =>
{
    if (options.ForceFail)
    {
        app.Logger.LogWarning("下游接口强制失败");
        return Results.Json(
            new { success = false, message = "下游服务故障" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new
    {
        success = true,
        message = "下游服务正常",
        timestamp = DateTimeOffset.UtcNow
    });
});

app.MapPost("/api/downstream/config", (DownstreamConfigRequest request, DownstreamOptions options) =>
{
    options.ForceFail = request.ForceFail;
    app.Logger.LogInformation("下游失败开关已设置为：{ForceFail}", options.ForceFail);
    return Results.Ok(new { options.ForceFail, message = "下游配置已更新" });
});

app.MapGet("/api/call", async (SimpleCircuitBreaker breaker, DownstreamOptions options) =>
{
    try
    {
        var result = await breaker.ExecuteAsync(async () =>
        {
            if (options.ForceFail)
            {
                throw new DownstreamException("下游服务故障");
            }

            await Task.Delay(10);
            return new
            {
                success = true,
                message = "经熔断器调用下游成功",
                timestamp = DateTimeOffset.UtcNow
            };
        });

        return Results.Ok(new
        {
            result,
            breaker = breaker.GetStatus()
        });
    }
    catch (CircuitOpenException ex)
    {
        app.Logger.LogWarning("熔断器打开，拒绝调用：{Message}", ex.Message);
        return Results.Json(
            new
            {
                success = false,
                message = ex.Message,
                breaker = breaker.GetStatus()
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (DownstreamException ex)
    {
        app.Logger.LogWarning("下游调用失败：{Message}", ex.Message);
        return Results.Json(
            new
            {
                success = false,
                message = ex.Message,
                breaker = breaker.GetStatus()
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/breaker", (SimpleCircuitBreaker breaker) => Results.Ok(breaker.GetStatus()));

app.MapPost("/api/breaker/reset", (SimpleCircuitBreaker breaker) =>
{
    breaker.Reset();
    app.Logger.LogInformation("熔断器已手动重置为 Closed");
    return Results.Ok(breaker.GetStatus());
});

app.Run();

sealed class DownstreamOptions
{
    public bool ForceFail { get; set; } = true;
}

record DownstreamConfigRequest(bool ForceFail);

sealed class DownstreamException(string message) : Exception(message);

sealed class CircuitOpenException(string message) : Exception(message);

enum BreakerState
{
    Closed,
    Open,
    HalfOpen
}

sealed class SimpleCircuitBreaker
{
    private readonly object _sync = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private BreakerState _state = BreakerState.Closed;
    private int _consecutiveFailures;
    private DateTimeOffset _openedAt;
    private int _halfOpenSuccesses;
    private int _totalCalls;
    private int _rejectedCalls;

    public SimpleCircuitBreaker(int failureThreshold = 3, int openSeconds = 10)
    {
        _failureThreshold = failureThreshold;
        _openDuration = TimeSpan.FromSeconds(openSeconds);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        lock (_sync)
        {
            _totalCalls++;
            TransitionIfNeeded_NoLock();

            if (_state == BreakerState.Open)
            {
                _rejectedCalls++;
                throw new CircuitOpenException($"熔断器处于 Open 状态，请约 {_openDuration.TotalSeconds} 秒后再试");
            }
        }

        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch
        {
            OnFailure();
            throw;
        }
    }

    public object GetStatus()
    {
        lock (_sync)
        {
            TransitionIfNeeded_NoLock();
            return new
            {
                state = _state.ToString(),
                consecutiveFailures = _consecutiveFailures,
                failureThreshold = _failureThreshold,
                openDurationSeconds = _openDuration.TotalSeconds,
                openedAt = _state == BreakerState.Closed ? (DateTimeOffset?)null : _openedAt,
                halfOpenSuccesses = _halfOpenSuccesses,
                totalCalls = _totalCalls,
                rejectedCalls = _rejectedCalls,
                message = _state switch
                {
                    BreakerState.Closed => "闭合：正常放行",
                    BreakerState.Open => "打开：拒绝调用",
                    BreakerState.HalfOpen => "半开：允许试探",
                    _ => "未知状态"
                }
            };
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _state = BreakerState.Closed;
            _consecutiveFailures = 0;
            _halfOpenSuccesses = 0;
            _openedAt = default;
        }
    }

    private void OnSuccess()
    {
        lock (_sync)
        {
            _consecutiveFailures = 0;
            if (_state == BreakerState.HalfOpen)
            {
                _halfOpenSuccesses++;
                if (_halfOpenSuccesses >= 1)
                {
                    _state = BreakerState.Closed;
                    _halfOpenSuccesses = 0;
                }
            }
        }
    }

    private void OnFailure()
    {
        lock (_sync)
        {
            _consecutiveFailures++;
            if (_state == BreakerState.HalfOpen || _consecutiveFailures >= _failureThreshold)
            {
                _state = BreakerState.Open;
                _openedAt = DateTimeOffset.UtcNow;
                _halfOpenSuccesses = 0;
            }
        }
    }

    private void TransitionIfNeeded_NoLock()
    {
        if (_state == BreakerState.Open && DateTimeOffset.UtcNow - _openedAt >= _openDuration)
        {
            _state = BreakerState.HalfOpen;
            _halfOpenSuccesses = 0;
        }
    }
}
