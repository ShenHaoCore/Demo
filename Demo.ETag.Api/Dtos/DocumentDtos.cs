namespace Demo.ETag.Api.Dtos;

/// <summary>文档输出 DTO。</summary>
public sealed class DocumentDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>全量更新文档输入 DTO。</summary>
public sealed class UpdateDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
