using Demo.Retry.Api.Application;
using Demo.Retry.Api.Dtos;
using Demo.Retry.Api.Services;
using Polly;
using Polly.Retry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<UnstableDownstream>();
builder.Services.AddSingleton<ResiliencePipeline<UnstableResultDto>>(CreateRetryPipeline);
builder.Services.AddSingleton<IRetryDemoAppService, RetryDemoAppService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();

static ResiliencePipeline<UnstableResultDto> CreateRetryPipeline(IServiceProvider sp)
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("RetryProxy");
    return new ResiliencePipelineBuilder<UnstableResultDto>()
        .AddRetry(new RetryStrategyOptions<UnstableResultDto>
        {
            MaxRetryAttempts = 4,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<UnstableResultDto>()
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
