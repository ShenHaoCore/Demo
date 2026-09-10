using Demo.ShortUrl.Api.Application;
using Demo.ShortUrl.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.ShortUrl.Api.Controllers;

[ApiController]
[Route("api/urls")]
public sealed class UrlsController(IShortUrlAppService shortUrlAppService) : ControllerBase
{
    [HttpPost]
    public ActionResult<ShortUrlCreatedDto> CreateAsync([FromBody] CreateShortUrlDto input)
    {
        try
        {
            var dto = shortUrlAppService.Create(input);
            return Created($"/api/urls/{dto.Code}", dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{code}")]
    public ActionResult<ShortUrlDto> GetAsync(string code)
    {
        var dto = shortUrlAppService.Get(code);
        if (dto is null)
        {
            return NotFound(new { message = "短码不存在", code });
        }

        return Ok(dto);
    }
}
