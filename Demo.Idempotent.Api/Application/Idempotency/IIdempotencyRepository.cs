namespace Demo.Idempotent.Api.Application.Idempotency;

/// <summary>
/// 幂等仓储（端口）。实现位于 Data；SaveChanges 由应用服务负责。
/// </summary>
public interface IIdempotencyRepository
{
    /// <summary>只读查找（过期视为 Miss，不删行）。</summary>
    Task<IdempotencyCheckResult> FindAsync(
        string key,
        string bodyHash,
        CancellationToken cancellationToken = default);

    /// <summary>写路径确认：过期则标记删除；否则按 Body 返回 Replay/Conflict/Miss。</summary>
    Task<IdempotencyCheckResult> FindForUpdateAsync(
        string key,
        string bodyHash,
        CancellationToken cancellationToken = default);

    /// <summary>附加已完成快照（由调用方 SaveChanges 提交）。</summary>
    void InsertCompleted(string key, IdempotencySnapshot snapshot, Guid orderId, TimeSpan ttl);
}
