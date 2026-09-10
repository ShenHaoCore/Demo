using Demo.Sso.Api.Application;
using Demo.Sso.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Sso.Api.Controllers;

[ApiController]
[Route("api/sso")]
public sealed class SsoController(ISsoAppService ssoAppService) : ControllerBase
{
    [HttpGet("clients")]
    public ActionResult<SsoClientListDto> GetClientsAsync() =>
        Ok(ssoAppService.GetClients());

    [HttpPost("login")]
    public ActionResult<SsoLoginResultDto> LoginAsync([FromBody] LoginDto input)
    {
        try
        {
            return Ok(ssoAppService.Login(input));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("ticket")]
    public ActionResult<SsoTicketResultDto> GetTicketAsync([FromQuery] string? service, [FromQuery] string? ssoSessionId)
    {
        try
        {
            return Ok(ssoAppService.IssueTicket(ssoSessionId ?? string.Empty, service ?? string.Empty));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("validate")]
    public ActionResult<SsoValidateResultDto> ValidateAsync([FromBody] ValidateDto input)
    {
        try
        {
            return Ok(ssoAppService.Validate(input));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
