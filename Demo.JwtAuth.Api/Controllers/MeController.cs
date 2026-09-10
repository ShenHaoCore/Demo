using System.Security.Claims;
using Demo.JwtAuth.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.JwtAuth.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class MeController : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentUserDto> GetAsync()
    {
        var name = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "未知";
        return Ok(new CurrentUserDto { Message = "当前用户", Username = name });
    }
}
