using Microsoft.Extensions.Options;

namespace Demo.Configuration.Api.Options;

/// <summary>启动时校验 Demo 配置，避免无效环境配置悄悄跑起来。</summary>
public sealed class DemoOptionsValidator : IValidateOptions<DemoOptions>
{
    public ValidateOptionsResult Validate(string? name, DemoOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.DisplayName))
        {
            errors.Add("Demo:DisplayName 不能为空");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            errors.Add("Demo:ApiKey 不能为空");
        }

        if (options.MaxRequestsPerMinute <= 0)
        {
            errors.Add("Demo:MaxRequestsPerMinute 必须大于 0");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
