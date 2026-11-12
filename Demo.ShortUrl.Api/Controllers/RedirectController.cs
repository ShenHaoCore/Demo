using Demo.ShortUrl.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace Demo.ShortUrl.Api.Controllers;

[ApiController]
public sealed class RedirectController(IShortUrlAppService shortUrlAppService) : ControllerBase
{
    [HttpGet("/{code}")]
    public async Task<IActionResult> RedirectAsync(string code, CancellationToken cancellationToken)
    {
        if (string.Equals(code, "health", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, "openapi", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, "scalar", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("api", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var longUrl = await shortUrlAppService.RedirectAndIncrementAsync(code, cancellationToken);
        if (longUrl is null)
        {
            return NotFound(new { message = "短码不存在", code });
        }

        // 短链用 302，避免 301 被永久缓存导致改落地页/计次失效
        return new RedirectResult(longUrl, permanent: false);
    }
}
