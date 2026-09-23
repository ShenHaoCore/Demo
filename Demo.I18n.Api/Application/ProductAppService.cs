using System.Globalization;
using Demo.I18n.Api.Dtos;
using Demo.I18n.Api.Entities;
using Demo.I18n.Api.Services;

namespace Demo.I18n.Api.Application;

/// <summary>按当前 UI 文化解析商品文案，带回退：精确匹配 → 父文化 → zh-CN → 任一翻译。</summary>
public sealed class ProductAppService(ProductStore store) : IProductAppService
{
    private const string FallbackCulture = "zh-CN";

    public IReadOnlyList<ProductDto> List() =>
        store.GetAll().Select(MapToDto).ToList();

    public ProductDto? Get(int id)
    {
        var product = store.Get(id);
        return product is null ? null : MapToDto(product);
    }

    private static ProductDto MapToDto(Product product)
    {
        var (translation, culture) = ResolveTranslation(product.Translations);
        return new ProductDto
        {
            Id = product.Id,
            Name = translation.Name,
            Description = translation.Description,
            Price = product.Price,
            Culture = culture
        };
    }

    private static (ProductTranslation Translation, string Culture) ResolveTranslation(
        IReadOnlyDictionary<string, ProductTranslation> translations)
    {
        if (translations.Count == 0)
        {
            return (
                new ProductTranslation { Name = string.Empty, Description = string.Empty },
                CultureInfo.CurrentUICulture.Name);
        }

        var requested = CultureInfo.CurrentUICulture.Name;

        if (translations.TryGetValue(requested, out var exact))
        {
            return (exact, NormalizeCultureKey(translations, requested));
        }

        // en-US → en 等父文化回退
        var parent = CultureInfo.CurrentUICulture.Parent;
        if (!string.IsNullOrEmpty(parent.Name) && translations.TryGetValue(parent.Name, out var parentMatch))
        {
            return (parentMatch, NormalizeCultureKey(translations, parent.Name));
        }

        if (translations.TryGetValue(FallbackCulture, out var zh))
        {
            return (zh, FallbackCulture);
        }

        var any = translations.First();
        return (any.Value, any.Key);
    }

    private static string NormalizeCultureKey(
        IReadOnlyDictionary<string, ProductTranslation> translations,
        string requested)
    {
        return translations.Keys.First(k =>
            string.Equals(k, requested, StringComparison.OrdinalIgnoreCase));
    }
}
