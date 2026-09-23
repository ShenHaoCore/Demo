using Demo.I18n.Api.Entities;

namespace Demo.I18n.Api.Services;

/// <summary>进程内商品种子数据（含 zh-CN / en 翻译）。</summary>
public sealed class ProductStore
{
    private readonly IReadOnlyDictionary<int, Product> _products;

    public ProductStore()
    {
        _products = new Dictionary<int, Product>
        {
            [1] = new Product
            {
                Id = 1,
                Price = 99.00m,
                Translations = new Dictionary<string, ProductTranslation>(StringComparer.OrdinalIgnoreCase)
                {
                    ["zh-CN"] = new ProductTranslation
                    {
                        Name = "无线鼠标",
                        Description = "人体工学 2.4G 无线鼠标"
                    },
                    ["en"] = new ProductTranslation
                    {
                        Name = "Wireless Mouse",
                        Description = "Ergonomic 2.4G wireless mouse"
                    }
                }
            },
            [2] = new Product
            {
                Id = 2,
                Price = 199.00m,
                Translations = new Dictionary<string, ProductTranslation>(StringComparer.OrdinalIgnoreCase)
                {
                    ["zh-CN"] = new ProductTranslation
                    {
                        Name = "机械键盘",
                        Description = "青轴 RGB 背光机械键盘"
                    },
                    ["en"] = new ProductTranslation
                    {
                        Name = "Mechanical Keyboard",
                        Description = "Blue-switch RGB backlit mechanical keyboard"
                    }
                }
            },
            [3] = new Product
            {
                Id = 3,
                Price = 49.90m,
                Translations = new Dictionary<string, ProductTranslation>(StringComparer.OrdinalIgnoreCase)
                {
                    ["zh-CN"] = new ProductTranslation
                    {
                        Name = "USB-C 数据线",
                        Description = "1 米快充编织线"
                    },
                    ["en"] = new ProductTranslation
                    {
                        Name = "USB-C Cable",
                        Description = "1m braided fast-charge cable"
                    }
                }
            }
        };
    }

    public IReadOnlyList<Product> GetAll() => _products.Values.OrderBy(p => p.Id).ToList();

    public Product? Get(int id) => _products.TryGetValue(id, out var product) ? product : null;
}
