using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var documents = new ConcurrentDictionary<string, Document>();
documents["doc-1"] = new Document("doc-1", "欢迎文档", "这是初始内容", DateTimeOffset.UtcNow);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/documents/{id}", (string id, HttpResponse response) =>
{
    if (!documents.TryGetValue(id, out var doc))
    {
        return Results.NotFound(new { message = $"未找到文档：{id}" });
    }

    var etag = ComputeETag(doc);
    response.Headers.ETag = etag;
    return Results.Ok(doc);
});

app.MapPut("/api/documents/{id}", async (string id, HttpRequest request, HttpResponse response) =>
{
    if (!documents.TryGetValue(id, out var existing))
    {
        return Results.NotFound(new { message = $"未找到文档：{id}" });
    }

    var ifMatch = request.Headers.IfMatch.ToString();
    if (string.IsNullOrWhiteSpace(ifMatch))
    {
        return Results.BadRequest(new { message = "PUT 请求必须提供 If-Match 请求头" });
    }

    var currentETag = ComputeETag(existing);
    var expected = NormalizeETag(ifMatch);
    if (!string.Equals(expected, currentETag, StringComparison.Ordinal))
    {
        app.Logger.LogWarning(
            "文档 {DocumentId} 更新冲突：If-Match={IfMatch}，当前 ETag={ETag}",
            id, ifMatch, currentETag);
        return Results.Json(
            new { message = "文档已被修改，请使用最新 ETag 重试", currentETag },
            statusCode: StatusCodes.Status412PreconditionFailed);
    }

    UpdateDocumentRequest? body;
    try
    {
        body = await request.ReadFromJsonAsync<UpdateDocumentRequest>();
    }
    catch (Exception)
    {
        return Results.BadRequest(new { message = "请求体不是有效的 JSON" });
    }

    if (body is null || string.IsNullOrWhiteSpace(body.Title) || body.Content is null)
    {
        return Results.BadRequest(new { message = "标题不能为空，内容字段必须提供" });
    }

    var updated = existing with
    {
        Title = body.Title.Trim(),
        Content = body.Content,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    documents[id] = updated;
    var newETag = ComputeETag(updated);
    response.Headers.ETag = newETag;
    app.Logger.LogInformation("已更新文档 {DocumentId}，新 ETag={ETag}", id, newETag);
    return Results.Ok(updated);
});

app.Run();

static string ComputeETag(Document doc)
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

static string NormalizeETag(string value)
{
    var trimmed = value.Trim();
    if (trimmed.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
    {
        trimmed = trimmed[2..].Trim();
    }

    return trimmed;
}

record Document(string Id, string Title, string Content, DateTimeOffset UpdatedAt);
record UpdateDocumentRequest(string Title, string Content);
