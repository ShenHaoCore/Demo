using Demo.QrLogin.Api.Dtos;

namespace Demo.QrLogin.Api.Application;

/// <summary>扫码登录应用服务契约。</summary>
public interface IQrLoginAppService
{
    QrTicketCreatedDto Create();

    /// <exception cref="InvalidOperationException">业务状态不允许。</exception>
    QrScanResultDto Scan(string ticketId, ScanDto? input);

    /// <exception cref="InvalidOperationException">业务状态不允许。</exception>
    QrConfirmResultDto Confirm(string ticketId);

    /// <returns>null 表示票据不存在。</returns>
    QrStatusDto? GetStatus(string ticketId);
}
