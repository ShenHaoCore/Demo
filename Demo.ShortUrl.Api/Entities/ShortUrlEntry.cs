namespace Demo.ShortUrl.Api.Entities;

/// <summary>短链实体。</summary>
public sealed class ShortUrlEntry
{
    public string Code { get; init; } = string.Empty;
    public string LongUrl { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public long VisitCount { get; set; }

    public ShortUrlEntry()
    {
    }

    public ShortUrlEntry(string code, string longUrl, DateTimeOffset createdAt, long visitCount)
    {
        Code = code;
        LongUrl = longUrl;
        CreatedAt = createdAt;
        VisitCount = visitCount;
    }
}
