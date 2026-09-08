using Asp.Versioning;
using Microsoft.OpenApi.Models;

namespace Demo.Versioning.Api.Extensions;

/// <summary>IServiceCollection 扩展：API 版本化与 OpenAPI。</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 API 版本化与 ApiExplorer。
    /// 版本读取：URL 段、Query（api-version）、Header（api-version）三者参数名一致。
    /// </summary>
    public static IServiceCollection AddVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = ApiVersions.V1Version;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader(ApiVersions.VersionParameterName),
                new HeaderApiVersionReader(ApiVersions.VersionParameterName));
        }).AddMvc().AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    /// <summary>按 API 版本注册独立 OpenAPI 文档。</summary>
    public static IServiceCollection AddVersionedOpenApiDocuments(this IServiceCollection services, string apiTitle = "Demo.Versioning.Api")
    {
        foreach (var (documentName, title) in ApiVersions.OpenApiDocuments)
        {
            services.AddOpenApi(documentName, options =>
            {
                options.ShouldInclude = description => string.Equals(description.GroupName, documentName, StringComparison.OrdinalIgnoreCase);
                options.AddDocumentTransformer((document, _, _) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = $"{apiTitle} ({title})",
                        Version = documentName
                    };
                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }
}
