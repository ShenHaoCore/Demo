namespace Demo.CacheProblems.Api.Dtos;

/// <summary>击穿场景应用层结果（可能映射为 409）。</summary>
public sealed class BreakdownResultDto
{
    public bool IsConflict { get; init; }

    public object Body { get; init; } = default!;
}
