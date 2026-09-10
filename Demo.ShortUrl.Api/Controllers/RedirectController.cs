using Demo.ShortUrl.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace Demo.ShortUrl.Api.Controllers;

[ApiController]
public sealed class RedirectController(IShortUrlAppService shortUrlAppService) : ControllerBase
{
    [HttpGet("/{code}")]
    public IActionResult RedirectAsync(string code)
    {
        if (string.Equals(code, "health", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, "openapi", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, "scalar", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("api", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var longUrl = shortUrlAppService.RedirectAndIncrement(code);
        if (longUrl is null)
        {
            return NotFound(new { message = "短码不存在", code });
        }

        return Redirect(longUrl);
    }
}
