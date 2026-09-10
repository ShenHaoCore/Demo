namespace Demo.OptimisticLock.Api.Entities;

/// <summary>商品实体（Domain）。</summary>
public sealed class Product
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Stock { get; init; }
    public long Version { get; init; }

    public Product()
    {
    }

    public Product(int id, string name, int stock, long version)
    {
        Id = id;
        Name = name;
        Stock = stock;
        Version = version;
    }
}
