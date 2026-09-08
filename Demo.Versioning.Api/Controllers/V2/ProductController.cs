using Asp.Versioning;
using Demo.Versioning.Api.Application;
using Demo.Versioning.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Versioning.Api.Controllers.V2;

/// <summary>
/// 产品 API v2：响应含 Description。
/// 支持 URL（/api/v2/products）与 Header/Query（/api/products + api-version）。
/// </summary>
[ApiController]
[ApiVersion(ApiVersions.V2)]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductController(IProductAppService productAppService) : ControllerBase
{
    private const bool IncludeDescription = true;

    [HttpGet]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductDto>>> GetListAsync()
    {
        var list = await productAppService.GetListAsync(IncludeDescription);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetAsync(Guid id)
    {
        var dto = await productAppService.GetAsync(id, IncludeDescription);
        if (dto is null)
        {
            return NotFound(new { message = "产品不存在", id });
        }

        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateAsync([FromBody] CreateProductDto input)
    {
        try
        {
            var dto = await productAppService.CreateAsync(input, IncludeDescription);
            return CreatedAtAction(nameof(GetAsync), new { id = dto.Id, version = ApiVersions.V2 }, dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
