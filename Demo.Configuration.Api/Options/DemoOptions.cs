namespace Demo.Configuration.Api.Options;

/// <summary>演示用业务配置，绑定自配置节 "Demo"。</summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>对外展示的环境标签（各环境 appsettings 中不同）。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>功能开关示例。</summary>
    public bool FeatureEnabled { get; set; }

    /// <summary>
    /// 演示密钥（仓库内均为假值）。可被环境变量 Demo__ApiKey 或 user-secrets 覆盖。
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>每分钟请求上限示例（须 &gt; 0，见 DemoOptionsValidator）。</summary>
    public int MaxRequestsPerMinute { get; set; }
}
