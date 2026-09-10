using Demo.JwtAuth.Api.Application;
using Demo.JwtAuth.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.JwtAuth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthAppService authAppService) : ControllerBase
{
    [HttpPost("login")]
    public ActionResult<TokenDto> LoginAsync([FromBody] LoginDto input)
    {
        try
        {
            return Ok(authAppService.Login(input));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public ActionResult<TokenDto> RefreshAsync([FromBody] RefreshDto input)
    {
        try
        {
            return Ok(authAppService.Refresh(input));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
