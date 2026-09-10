using Demo.Sso.Api.Dtos;

namespace Demo.Sso.Api.Application;

/// <summary>SSO 应用服务契约。</summary>
public interface ISsoAppService
{
    SsoClientListDto GetClients();

    /// <exception cref="ArgumentException">输入校验失败。</exception>
    SsoLoginResultDto Login(LoginDto input);

    /// <exception cref="ArgumentException">输入校验失败。</exception>
    /// <exception cref="InvalidOperationException">会话或 service 无效。</exception>
    SsoTicketResultDto IssueTicket(string ssoSessionId, string service);

    /// <exception cref="ArgumentException">输入校验失败。</exception>
    /// <exception cref="InvalidOperationException">ticket 无效。</exception>
    SsoValidateResultDto Validate(ValidateDto input);
}
