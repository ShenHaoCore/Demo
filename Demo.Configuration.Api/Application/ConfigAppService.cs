using Demo.Configuration.Api.Dtos;
using Demo.Configuration.Api.Options;
using Microsoft.Extensions.Options;

namespace Demo.Configuration.Api.Application;

/// <summary>读取当前环境生效的配置快照、键来源与配置提供程序列表。</summary>
public sealed class ConfigAppService(
    IHostEnvironment hostEnvironment,
    IConfiguration configuration,
    IOptions<DemoOptions> options) : IConfigAppService
{
    private static readonly string[] TrackedKeys =
    [
        "Demo:DisplayName",
        "Demo:FeatureEnabled",
        "Demo:ApiKey",
        "Demo:MaxRequestsPerMinute"
    ];

    public ConfigSnapshotDto GetSnapshot()
    {
        var demo = options.Value;
        var root = (IConfigurationRoot)configuration;
        var sources = TrackedKeys.ToDictionary(
            key => key,
            key => ResolveProviderName(root, key) ?? "(未找到)");

        return new ConfigSnapshotDto
        {
            EnvironmentName = hostEnvironment.EnvironmentName,
            IsDevelopment = hostEnvironment.IsDevelopment(),
            IsProduction = hostEnvironment.IsProduction(),
            DisplayName = demo.DisplayName,
            FeatureEnabled = demo.FeatureEnabled,
            ApiKey = MaskApiKey(demo.ApiKey),
            MaxRequestsPerMinute = demo.MaxRequestsPerMinute,
            ValueSources = sources,
            Message =
                $"当前环境={hostEnvironment.EnvironmentName}；" +
                "加载顺序：appsettings.json → appsettings.{{Env}}.json → User Secrets(仅 Development) → 环境变量 → 命令行；" +
                "ValueSources 标明各键最终来自哪个提供程序。"
        };
    }

    public ConfigProvidersResultDto GetProviders()
    {
        var providers = ((IConfigurationRoot)configuration).Providers
            .Select(p => new ConfigProviderDto { Name = p.ToString() ?? p.GetType().Name })
            .ToList();

        return new ConfigProvidersResultDto
        {
            EnvironmentName = hostEnvironment.EnvironmentName,
            Providers = providers,
            Message = "提供程序按注册顺序排列；解析某键时从后往前找，先命中者生效（同名键后面的覆盖前面的）。"
        };
    }

    /// <summary>与 ConfigurationRoot 解析规则一致：从后往前找第一个含有该键的提供程序。</summary>
    private static string? ResolveProviderName(IConfigurationRoot root, string key)
    {
        foreach (var provider in root.Providers.Reverse())
        {
            if (provider.TryGet(key, out _))
            {
                return provider.ToString() ?? provider.GetType().Name;
            }
        }

        return null;
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return string.Empty;
        }

        if (apiKey.Length <= 4)
        {
            return "****";
        }

        return apiKey[..2] + new string('*', apiKey.Length - 4) + apiKey[^2..];
    }
}
