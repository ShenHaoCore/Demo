using Demo.I18n.Api.Application;
using Demo.I18n.Api.Dtos;
using Demo.I18n.Api.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Demo.I18n.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(
    IProductAppService productAppService,
    IStringLocalizer<SharedResources> localizer) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListProducts")]
    [EndpointSummary("商品列表：名称/描述随当前文化返回")]
    public ActionResult<IReadOnlyList<ProductDto>> List() => Ok(productAppService.List());

    [HttpGet("{id:int}")]
    [EndpointName("GetProduct")]
    [EndpointSummary("商品详情：数据国际化 + 未找到时本地化错误消息")]
    public ActionResult<ProductDto> Get(int id)
    {
        var product = productAppService.Get(id);
        if (product is null)
        {
            return NotFound(new
            {
                message = localizer["ProductNotFound"].Value,
                id
            });
        }

        return Ok(product);
    }
}
