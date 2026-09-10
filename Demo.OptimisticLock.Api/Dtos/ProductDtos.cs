namespace Demo.OptimisticLock.Api.Dtos;

/// <summary>商品输出 DTO。</summary>
public sealed class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
    public long Version { get; set; }
}

/// <summary>创建商品输入 DTO。</summary>
public sealed class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
}

/// <summary>乐观锁更新库存输入 DTO。</summary>
public sealed class UpdateStockDto
{
    public int Stock { get; set; }
    public long ExpectedVersion { get; set; }
}
