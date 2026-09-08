using Scalar.AspNetCore;

namespace Demo.Versioning.Api.Extensions;

/// <summary>WebApplication 扩展：OpenAPI 与 Scalar。</summary>
public static class WebApplicationExtensions
{
    /// <summary>映射 OpenAPI 端点，并在 Scalar 中注册各版本文档。</summary>
    public static WebApplication MapVersionedOpenApiAndScalar(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            foreach (var (documentName, title) in ApiVersions.OpenApiDocuments)
            {
                options.AddDocument(documentName, title);
            }
        });

        return app;
    }
}
