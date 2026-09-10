namespace Demo.ETag.Api.Entities;

/// <summary>文档实体（Domain）。</summary>
public sealed class Document
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; init; }

    public Document()
    {
    }

    public Document(string id, string title, string content, DateTimeOffset updatedAt)
    {
        Id = id;
        Title = title;
        Content = content;
        UpdatedAt = updatedAt;
    }
}
