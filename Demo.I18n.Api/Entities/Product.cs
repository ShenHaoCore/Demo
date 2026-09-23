namespace Demo.I18n.Api.Entities;

/// <summary>商品实体：价格等非文案字段 + 多语言翻译字典。</summary>
public sealed class Product
{
    public required int Id { get; init; }
    public required decimal Price { get; init; }
    public required IReadOnlyDictionary<string, ProductTranslation> Translations { get; init; }
}

public sealed class ProductTranslation
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}
