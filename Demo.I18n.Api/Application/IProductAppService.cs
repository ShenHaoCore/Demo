using Demo.I18n.Api.Dtos;

namespace Demo.I18n.Api.Application;

public interface IProductAppService
{
    IReadOnlyList<ProductDto> List();
    ProductDto? Get(int id);
}
