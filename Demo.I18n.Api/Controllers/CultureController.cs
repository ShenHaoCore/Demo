using System.Globalization;
using Demo.I18n.Api.Resources;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Demo.I18n.Api.Controllers;

[ApiController]
[Route("api/culture")]
public sealed class CultureController(
    IStringLocalizer<SharedResources> localizer,
    IOptions<RequestLocalizationOptions> localizationOptions) : ControllerBase
{
    [HttpGet]
    [EndpointName("GetCulture")]
    [EndpointSummary("查看当前请求文化（验证 ?culture= / Accept-Language）")]
    public IActionResult Get()
    {
        var feature = HttpContext.Features.Get<IRequestCultureFeature>();
        var options = localizationOptions.Value;
        return Ok(new
        {
            culture = CultureInfo.CurrentCulture.Name,
            uiCulture = CultureInfo.CurrentUICulture.Name,
            requestCulture = feature?.RequestCulture.Culture.Name,
            provider = feature?.Provider?.GetType().Name,
            supportedCultures = options.SupportedCultures?.Select(c => c.Name).ToArray(),
            message = localizer["SupportedCultures"].Value
        });
    }
}
