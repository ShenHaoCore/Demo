using System.Globalization;
using Demo.I18n.Api.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Demo.I18n.Api.Controllers;

[ApiController]
[Route("api/messages")]
public sealed class MessagesController(IStringLocalizer<SharedResources> localizer) : ControllerBase
{
    [HttpGet("welcome")]
    [EndpointName("GetWelcomeMessage")]
    [EndpointSummary("消息本地化：欢迎语随文化切换")]
    public IActionResult Welcome()
    {
        return Ok(new
        {
            message = localizer["Welcome"].Value,
            culturesHint = localizer["SupportedCultures"].Value,
            culture = CultureInfo.CurrentUICulture.Name
        });
    }
}
