namespace Demo.Sso.Api.Entities;

/// <summary>SSO 演示客户端。</summary>
public sealed class SsoClient
{
    public string Name { get; init; } = string.Empty;
    public string ServiceUrl { get; init; } = string.Empty;

    public SsoClient()
    {
    }

    public SsoClient(string name, string serviceUrl)
    {
        Name = name;
        ServiceUrl = serviceUrl;
    }
}
