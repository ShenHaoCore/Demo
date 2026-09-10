using Demo.ETag.Api.Dtos;

namespace Demo.ETag.Api.Application;

/// <summary>带 ETag 的文档查询结果。</summary>
public sealed class DocumentETagResult
{
    public required DocumentDto Document { get; init; }
    public required string ETag { get; init; }
}

/// <summary>文档更新结果状态。</summary>
public enum DocumentUpdateStatus
{
    Success,
    NotFound,
    PreconditionFailed,
    BadRequest
}

/// <summary>文档更新结果。</summary>
public sealed class DocumentUpdateResult
{
    public DocumentUpdateStatus Status { get; init; }
    public DocumentDto? Document { get; init; }
    public string? ETag { get; init; }
    public string? CurrentETag { get; init; }
    public string? Message { get; init; }
}

/// <summary>文档应用服务契约。</summary>
public interface IDocumentAppService
{
    Task<DocumentETagResult?> GetAsync(string id);

    Task<DocumentUpdateResult> UpdateAsync(string id, UpdateDocumentDto input, string? ifMatch);
}
