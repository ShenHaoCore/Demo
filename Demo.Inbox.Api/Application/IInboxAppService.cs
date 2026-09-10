namespace Demo.Inbox.Api.Application;

/// <summary>Inbox 应用服务契约。</summary>
public interface IInboxAppService
{
    bool IsProcessed(string messageId);

    bool TryProcess(string messageId, string? payload, out bool alreadyProcessed);

    void IncrementDedup();

    object GetStats();
}
