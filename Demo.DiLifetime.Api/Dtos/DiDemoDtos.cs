namespace Demo.DiLifetime.Api.Dtos;

/// <summary>DI 生命周期演示输出。</summary>
public sealed class DiDemoResultDto
{
    public string Message { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DiResolveIdsDto FirstResolve { get; set; } = new();
    public DiResolveIdsDto SecondResolve { get; set; } = new();
    public DiExplanationDto Explanation { get; set; } = new();
}

public sealed class DiResolveIdsDto
{
    public Guid Singleton { get; set; }
    public Guid Scoped { get; set; }
    public Guid Transient { get; set; }
}

public sealed class DiExplanationDto
{
    public string Transient { get; set; } = string.Empty;
    public string Scoped { get; set; } = string.Empty;
    public string Singleton { get; set; } = string.Empty;
}
