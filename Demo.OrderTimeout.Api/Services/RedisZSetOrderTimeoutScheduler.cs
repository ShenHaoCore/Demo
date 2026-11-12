using Demo.OrderTimeout.Api.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Demo.OrderTimeout.Api.Services;

/// <summary>
/// Redis ZSET 延迟队列：member=orderId，score=到期 Unix 毫秒。
/// 到期由 <see cref="RedisZSetOrderTimeoutService"/> 轮询弹出。
/// </summary>
public sealed class RedisZSetOrderTimeoutScheduler(
    IConnectionMultiplexer mux,
    IOptions<OrderTimeoutOptions> options) : IOrderTimeoutScheduler
{
    private readonly IDatabase _db = mux.GetDatabase();
    private readonly string _key = options.Value.RedisKey;

    public string ProviderName => "Redis";

    public string Key => _key;

    public void Schedule(Guid orderId, TimeSpan delay, Action onTimeout)
    {
        // onTimeout 由轮询服务统一 CAS 关单；跨进程时本机回调无意义。
        _ = onTimeout;
        var expireAtMs = DateTimeOffset.UtcNow.Add(delay).ToUnixTimeMilliseconds();
        _db.SortedSetAdd(_key, orderId.ToString("D"), expireAtMs);
    }

    public void Cancel(Guid orderId) =>
        _db.SortedSetRemove(_key, orderId.ToString("D"));

    /// <summary>取出并 ZREM 抢占已到期成员（多实例下谁 Remove 成功谁处理）。</summary>
    public IReadOnlyList<Guid> ClaimExpired(int take = 100)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var due = _db.SortedSetRangeByScore(
            _key,
            start: double.NegativeInfinity,
            stop: now,
            take: take);

        if (due.Length == 0)
        {
            return [];
        }

        var claimed = new List<Guid>(due.Length);
        foreach (var member in due)
        {
            if (!_db.SortedSetRemove(_key, member))
            {
                continue;
            }

            if (Guid.TryParse(member.ToString(), out var orderId))
            {
                claimed.Add(orderId);
            }
        }

        return claimed;
    }
}
