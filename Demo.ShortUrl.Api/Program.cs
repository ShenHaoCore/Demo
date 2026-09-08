using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ShortUrlStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Demo.ShortUrl.Api" }));

app.MapPost("/api/urls", (CreateShortUrlRequest request, ShortUrlStore store) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.LongUrl))
    {
        return Results.BadRequest(new { message = "请提供有效的 longUrl" });
    }

    if (!Uri.TryCreate(request.LongUrl.Trim(), UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        return Results.BadRequest(new { message = "longUrl 必须是有效的 http/https 绝对地址" });
    }

    var entry = store.Create(uri.ToString());
    return Results.Created($"/api/urls/{entry.Code}", new
    {
        code = entry.Code,
        shortPath = $"/{entry.Code}",
        longUrl = entry.LongUrl,
        createdAt = entry.CreatedAt,
        visitCount = entry.VisitCount,
        message = "短链已创建"
    });
})
.WithName("CreateShortUrl")
.WithSummary("将长链接缩短为短码");

app.MapGet("/api/urls/{code}", (string code, ShortUrlStore store) =>
{
    if (!store.TryGet(code, out var entry))
    {
        return Results.NotFound(new { message = "短码不存在", code });
    }

    return Results.Ok(new
    {
        code = entry.Code,
        longUrl = entry.LongUrl,
        createdAt = entry.CreatedAt,
        visitCount = entry.VisitCount
    });
})
.WithName("GetShortUrlMeta")
.WithSummary("查询短链元数据（含访问计数）");

app.MapGet("/{code}", (string code, ShortUrlStore store) =>
{
    if (string.Equals(code, "health", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "openapi", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "scalar", StringComparison.OrdinalIgnoreCase) ||
        code.StartsWith("api", StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound();
    }

    if (!store.TryGet(code, out var entry))
    {
        return Results.NotFound(new { message = "短码不存在", code });
    }

    store.IncrementVisit(code);
    return Results.Redirect(entry.LongUrl, permanent: false);
})
.WithName("RedirectShortUrl")
.WithSummary("按短码 302 跳转到长链接");

app.Run();

sealed class ShortUrlStore
{
    private readonly ConcurrentDictionary<string, ShortUrlEntry> _urls = new(StringComparer.Ordinal);
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public ShortUrlEntry Create(string longUrl)
    {
        string code;
        do
        {
            code = GenerateCode(7);
        } while (!_urls.TryAdd(code, new ShortUrlEntry(code, longUrl, DateTimeOffset.UtcNow, 0)));

        return _urls[code];
    }

    public bool TryGet(string code, out ShortUrlEntry entry) =>
        _urls.TryGetValue(code, out entry!);

    public void IncrementVisit(string code)
    {
        _urls.AddOrUpdate(
            code,
            _ => throw new InvalidOperationException("短码不存在"),
            (_, existing) => existing with { VisitCount = existing.VisitCount + 1 });
    }

    private static string GenerateCode(int length)
    {
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            // GetInt32 均匀分布，避免 bytes[i] % Alphabet.Length 的 modulo bias
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }
}

record CreateShortUrlRequest(string LongUrl);

record ShortUrlEntry(string Code, string LongUrl, DateTimeOffset CreatedAt, long VisitCount);
