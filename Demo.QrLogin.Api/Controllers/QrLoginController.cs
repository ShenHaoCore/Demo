using Demo.QrLogin.Api.Application;
using Demo.QrLogin.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Demo.QrLogin.Api.Controllers;

[ApiController]
[Route("api/qr")]
public sealed class QrLoginController(IQrLoginAppService qrLoginAppService) : ControllerBase
{
    [HttpPost("create")]
    public ActionResult<QrTicketCreatedDto> CreateAsync() =>
        Ok(qrLoginAppService.Create());

    [HttpPost("{ticket}/scan")]
    public ActionResult<QrScanResultDto> ScanAsync(
        string ticket,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ScanDto? input)
    {
        try
        {
            return Ok(qrLoginAppService.Scan(ticket, input));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{ticket}/confirm")]
    public ActionResult<QrConfirmResultDto> ConfirmAsync(string ticket)
    {
        try
        {
            return Ok(qrLoginAppService.Confirm(ticket));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{ticket}/status")]
    public ActionResult<QrStatusDto> GetStatusAsync(string ticket)
    {
        var dto = qrLoginAppService.GetStatus(ticket);
        if (dto is null)
        {
            return NotFound(new { message = "票据不存在" });
        }

        return Ok(dto);
    }
}
