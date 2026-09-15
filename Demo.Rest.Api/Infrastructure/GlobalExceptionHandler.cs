using Microsoft.AspNetCore.Diagnostics;

namespace Demo.Rest.Api.Infrastructure;

/// <summary>
/// 全局异常处理
/// </summary>
/// <param name="logger">日志记录器</param>
/// <param name="environment">主机环境</param>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    /// <summary>
    /// 处理异常
    /// </summary>
    /// <param name="httpContext">HTTP上下文</param>
    /// <param name="exception">异常</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否处理成功</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, environment.IsDevelopment() ? exception.Message : "服务器内部错误")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError) { logger.LogError(exception, "未处理异常"); }
        else { logger.LogWarning(exception, "业务异常：{Message}", message); }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
        return true;
    }
}
