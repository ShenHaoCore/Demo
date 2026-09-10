using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Demo.ETag.Api.Dtos;
using Demo.ETag.Api.Entities;

namespace Demo.ETag.Api.Application;

/// <summary>文档应用服务。</summary>
public sealed class DocumentAppService : IDocumentAppService
{
    private readonly ConcurrentDictionary<string, Document> _documents = new();

    public DocumentAppService()
    {
        _documents["doc-1"] = new Document("doc-1", "欢迎文档", "这是初始内容", DateTimeOffset.UtcNow);
    }

    public Task<DocumentETagResult?> GetAsync(string id)
    {
        if (!_documents.TryGetValue(id, out var entity))
        {
            return Task.FromResult<DocumentETagResult?>(null);
        }

        return Task.FromResult<DocumentETagResult?>(new DocumentETagResult
        {
            Document = MapToDto(entity),
            ETag = ComputeETag(entity)
        });
    }

    public Task<DocumentUpdateResult> UpdateAsync(string id, UpdateDocumentDto input, string? ifMatch)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.Title) || input.Content is null)
        {
            return Task.FromResult(new DocumentUpdateResult
            {
                Status = DocumentUpdateStatus.BadRequest,
                Message = "标题不能为空，内容字段必须提供"
            });
        }

        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return Task.FromResult(new DocumentUpdateResult
            {
                Status = DocumentUpdateStatus.BadRequest,
                Message = "PUT 请求必须提供 If-Match 请求头"
            });
        }

        var expected = NormalizeETag(ifMatch);

        // CAS：校验 If-Match 与写入必须基于同一快照，避免双请求都返回 200
        while (true)
        {
            if (!_documents.TryGetValue(id, out var existing))
            {
                return Task.FromResult(new DocumentUpdateResult
                {
                    Status = DocumentUpdateStatus.NotFound
                });
            }

            var currentETag = ComputeETag(existing);
            if (!string.Equals(expected, currentETag, StringComparison.Ordinal))
            {
                return Task.FromResult(new DocumentUpdateResult
                {
                    Status = DocumentUpdateStatus.PreconditionFailed,
                    CurrentETag = currentETag,
                    Message = "文档已被修改，请使用最新 ETag 重试"
                });
            }

            var updated = new Document(
                existing.Id,
                input.Title.Trim(),
                input.Content,
                DateTimeOffset.UtcNow);

            if (_documents.TryUpdate(id, updated, existing))
            {
                var newETag = ComputeETag(updated);
                return Task.FromResult(new DocumentUpdateResult
                {
                    Status = DocumentUpdateStatus.Success,
                    Document = MapToDto(updated),
                    ETag = newETag
                });
            }
        }
    }

    private static DocumentDto MapToDto(Document entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Content = entity.Content,
        UpdatedAt = entity.UpdatedAt
    };

    private static string ComputeETag(Document doc)
    {
        var payload = JsonSerializer.Serialize(new
        {
            doc.Id,
            doc.Title,
            doc.Content,
            UpdatedAt = doc.UpdatedAt.ToUnixTimeMilliseconds()
        });
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return $"\"{Convert.ToHexString(hash)[..16]}\"";
    }

    private static string NormalizeETag(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..].Trim();
        }

        return trimmed;
    }
}
