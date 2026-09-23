namespace Demo.Configuration.Api.Dtos;

public sealed class ConfigSnapshotDto
{
    public required string EnvironmentName { get; init; }
    public required bool IsDevelopment { get; init; }
    public required bool IsProduction { get; init; }
    public required string DisplayName { get; init; }
    public required bool FeatureEnabled { get; init; }
    public required string ApiKey { get; init; }
    public required int MaxRequestsPerMinute { get; init; }
    /// <summary>各 Demo 配置键实际命中的提供程序（教学：谁覆盖了谁）。</summary>
    public required IReadOnlyDictionary<string, string> ValueSources { get; init; }
    public required string Message { get; init; }
}

public sealed class ConfigProviderDto
{
    public required string Name { get; init; }
}

public sealed class ConfigProvidersResultDto
{
    public required string EnvironmentName { get; init; }
    public required IReadOnlyList<ConfigProviderDto> Providers { get; init; }
    public required string Message { get; init; }
}
