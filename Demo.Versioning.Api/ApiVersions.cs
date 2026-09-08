using Asp.Versioning;

namespace Demo.Versioning.Api;

/// <summary>API 版本号与 OpenAPI 文档名统一管理。</summary>
public static class ApiVersions
{
    public const string V1 = "1.0";
    public const string V2 = "2.0";

    public const int V1Major = 1;
    public const int V2Major = 2;

    public const string V1Document = "v1";
    public const string V2Document = "v2";

    /// <summary>Header / Query 共用的版本参数名。</summary>
    public const string VersionParameterName = "api-version";

    public static ApiVersion V1Version { get; } = new(V1Major, 0);
    public static ApiVersion V2Version { get; } = new(V2Major, 0);

    public static IReadOnlyList<(string DocumentName, string Title)> OpenApiDocuments { get; } =
    [
        (V1Document, "API V1"),
        (V2Document, "API V2")
    ];
}
