using Demo.CacheProblems.Api.Dtos;

namespace Demo.CacheProblems.Api.Application;

/// <summary>缓存问题演示应用服务契约。</summary>
public interface ICacheDemoAppService
{
    Task<object> GetPenetrationAsync(string? id, bool? cacheNull);

    Task<BreakdownResultDto> GetBreakdownAsync(bool? useLock);

    Task<object> GetAvalancheAsync(bool? jitter, int? baseSeconds);

    Task ExpireHotKeyAsync();
}
