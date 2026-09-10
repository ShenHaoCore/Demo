using Demo.OptimisticLock.Api.Application;
using Demo.OptimisticLock.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.OptimisticLock.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductAppService productAppService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [EndpointName("GetProduct")]
    [EndpointSummary("查询商品库存与当前 version")]
    public async Task<IActionResult> GetAsync(int id)
    {
        var product = await productAppService.GetAsync(id);
        if (product is null)
        {
            return NotFound(new { message = "商品不存在", id });
        }

        return Ok(new
        {
            id = product.Id,
            name = product.Name,
            stock = product.Stock,
            version = product.Version,
            message = "返回当前 version，更新库存时请带上 expectedVersion"
        });
    }

    [HttpPut("{id:int}/stock")]
    [EndpointName("UpdateStockOptimistic")]
    [EndpointSummary("带 expectedVersion 的 CAS 更新库存，冲突返回 409")]
    public async Task<IActionResult> UpdateStockAsync(int id, [FromBody] UpdateStockDto input)
    {
        if (input is null || input.Stock < 0)
        {
            return BadRequest(new { message = "stock 必须 >= 0，并提供 expectedVersion" });
        }

        var result = await productAppService.UpdateStockAsync(id, input);
        return result.Status switch
        {
            UpdateStatus.NotFound => NotFound(new { message = "商品不存在", id }),
            UpdateStatus.Conflict => Conflict(new
            {
                message = "乐观锁冲突：expectedVersion 与当前 version 不一致",
                id,
                expectedVersion = input.ExpectedVersion,
                currentVersion = result.Product?.Version,
                currentStock = result.Product?.Stock
            }),
            UpdateStatus.Success => Ok(new
            {
                message = "CAS 更新成功，version 已递增",
                id = result.Product!.Id,
                name = result.Product.Name,
                stock = result.Product.Stock,
                version = result.Product.Version
            }),
            _ => StatusCode(500)
        };
    }

    [HttpPost]
    [EndpointName("CreateProduct")]
    [EndpointSummary("创建带 version 的商品")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateProductDto input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Name) || input.Stock < 0)
        {
            return BadRequest(new { message = "请提供 name 与 stock（>=0）" });
        }

        var product = await productAppService.CreateAsync(input);
        return Created($"/api/products/{product.Id}", new
        {
            id = product.Id,
            name = product.Name,
            stock = product.Stock,
            version = product.Version
        });
    }
}
