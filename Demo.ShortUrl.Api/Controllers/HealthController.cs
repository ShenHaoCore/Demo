using Demo.ShortUrl.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Demo.ShortUrl.Api.Controllers;

[ApiController]
public sealed class HealthController(IShortUrlStore store) : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "Demo.ShortUrl.Api",
        store = store.ProviderName
    });
}
