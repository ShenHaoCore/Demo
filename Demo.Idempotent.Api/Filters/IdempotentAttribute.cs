using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Demo.Idempotent.Api.Filters;

/// <summary>
/// 声明式幂等协议：校验 Idempotency-Key、计算 Body 指纹。
/// 业务回放/冲突/落库由应用服务决策。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute, IAsyncResourceFilter
{
    public const string HeaderName = "Idempotency-Key";

    private const string KeyItem = "Idempotency.Context.Key";
    private const string HashItem = "Idempotency.Context.BodyHash";

    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!request.Headers.TryGetValue(HeaderName, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            context.Result = new BadRequestObjectResult(new { message = $"缺少必填请求头 {HeaderName}" });
            return;
        }

        var key = keyValues.ToString().Trim();
        request.EnableBuffering();
        string bodyText;
        using (var reader = new StreamReader(
                   request.Body,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: false,
                   leaveOpen: true))
        {
            bodyText = await reader.ReadToEndAsync(context.HttpContext.RequestAborted);
            request.Body.Position = 0;
        }

        context.HttpContext.Items[KeyItem] = key;
        context.HttpContext.Items[HashItem] = IdempotencyBodyHasher.ComputeHash(bodyText);

        context.HttpContext.Response.OnStarting(() =>
        {
            context.HttpContext.Response.Headers[HeaderName] = key;
            return Task.CompletedTask;
        });

        await next();
    }

    /// <summary>读取 Filter 写入的幂等上下文。</summary>
    public static bool TryGetContext(HttpContext httpContext, out string key, out string bodyHash)
    {
        if (httpContext.Items[KeyItem] is string k &&
            !string.IsNullOrWhiteSpace(k) &&
            httpContext.Items[HashItem] is string h &&
            !string.IsNullOrWhiteSpace(h))
        {
            key = k;
            bodyHash = h;
            return true;
        }

        key = string.Empty;
        bodyHash = string.Empty;
        return false;
    }
}
