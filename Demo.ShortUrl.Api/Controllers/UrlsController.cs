using Demo.ShortUrl.Api.Application;
using Demo.ShortUrl.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.ShortUrl.Api.Controllers;

[ApiController]
[Route("api/urls")]
public sealed class UrlsController(IShortUrlAppService shortUrlAppService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ShortUrlCreatedDto>> CreateAsync(
        [FromBody] CreateShortUrlDto input,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await shortUrlAppService.CreateAsync(input, cancellationToken);
            return Created($"/api/urls/{dto.Code}", dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<ShortUrlDto>> GetAsync(string code, CancellationToken cancellationToken)
    {
        var dto = await shortUrlAppService.GetAsync(code, cancellationToken);
        if (dto is null)
        {
            return NotFound(new { message = "短码不存在", code });
        }

        return Ok(dto);
    }
}
