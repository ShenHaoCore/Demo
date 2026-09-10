using Demo.Redis.Api.Application;
using Demo.Redis.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Redis.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductAppService productAppService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [EndpointName("GetProduct")]
    [EndpointSummary("Cache-Aside：先查缓存，未命中读 DB 再回填")]
    public ActionResult<ProductGetResultDto> GetAsync(int id)
    {
        var result = productAppService.Get(id);
        if (result is null)
        {
            return NotFound(new { message = "商品不存在", id, cacheHit = false, source = "db" });
        }

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [EndpointName("UpdateProduct")]
    [EndpointSummary("更新 DB 并删除缓存")]
    public ActionResult<ProductUpdateResultDto> UpdateAsync(int id, [FromBody] UpdateProductDto input)
    {
        try
        {
            return Ok(productAppService.Update(id, input));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
