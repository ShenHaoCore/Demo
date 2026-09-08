namespace Demo.Versioning.Api.Dtos;

/// <summary>
/// 统一产品输出 DTO。
/// 教学取舍：共用一个类型；V1 通过不填充 Description + WhenWritingNull 省略字段，
/// 面试口述需说明生产更常见的做法是按版本拆分契约 DTO。
/// </summary>
public sealed class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
}

/// <summary>创建产品输入 DTO。V1 忽略 Description。</summary>
public sealed class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
}
