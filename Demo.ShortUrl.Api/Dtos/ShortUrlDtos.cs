namespace Demo.ShortUrl.Api.Dtos;

/// <summary>创建短链输入。</summary>
public sealed class CreateShortUrlDto
{
    public string LongUrl { get; set; } = string.Empty;
}

/// <summary>短链输出。</summary>
public sealed class ShortUrlDto
{
    public string Code { get; set; } = string.Empty;
    public string LongUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public long VisitCount { get; set; }
}

/// <summary>创建短链输出（含提示）。</summary>
public sealed class ShortUrlCreatedDto
{
    public string Code { get; set; } = string.Empty;
    public string ShortPath { get; set; } = string.Empty;
    public string LongUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public long VisitCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
