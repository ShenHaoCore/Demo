namespace Demo.CacheProblems.Api.Entities;

/// <summary>缓存条目（含空值标记，用于演示穿透防护）。</summary>
public record CacheItem(bool Exists, object? Data);
