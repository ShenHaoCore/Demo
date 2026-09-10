namespace Demo.Redis.Api.Entities;

/// <summary>商品实体。</summary>
public sealed class Product
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }

    public Product()
    {
    }

    public Product(int id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }
}
