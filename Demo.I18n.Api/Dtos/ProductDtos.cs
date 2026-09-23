namespace Demo.I18n.Api.Dtos;

public sealed class ProductDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required string Culture { get; init; }
}
