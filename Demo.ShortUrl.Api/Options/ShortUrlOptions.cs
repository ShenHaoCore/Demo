namespace Demo.ShortUrl.Api.Options;

/// <summary>短链存储与发号配置。</summary>
public sealed class ShortUrlOptions
{
    public const string SectionName = "ShortUrl";

    /// <summary>Memory | Redis。生产用 Redis。</summary>
    public string Provider { get; set; } = "Memory";

    public string Redis { get; set; } = "localhost:6379";

    /// <summary>Base62 短码最小长度（左侧补 0）。</summary>
    public int CodeLength { get; set; } = 6;
}
