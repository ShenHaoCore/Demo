using System.Text.Json.Serialization;

namespace Demo.Redis.Api.Dtos;

/// <summary>商品输出。</summary>
public sealed class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>更新商品输入。</summary>
public sealed class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>缓存统计输出。</summary>
public sealed class CacheStatsDto
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public int Entries { get; set; }
}

/// <summary>Cache-Aside 读取结果。</summary>
public sealed class ProductGetResultDto
{
    public string Source { get; set; } = string.Empty;
    public bool CacheHit { get; set; }
    public ProductDto? Product { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
}

/// <summary>更新商品结果。</summary>
public sealed class ProductUpdateResultDto
{
    public string Message { get; set; } = string.Empty;
    public ProductDto Product { get; set; } = new();
    public string CacheKeyRemoved { get; set; } = string.Empty;
}

/// <summary>缓存统计接口输出。</summary>
public sealed class CacheStatsResultDto
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public int Entries { get; set; }
    public string Message { get; set; } = string.Empty;
}
