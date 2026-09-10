namespace Demo.DistributedLock.Api.Dtos;

/// <summary>获取锁输入。</summary>
public record AcquireLockDto(int? TtlSeconds);

/// <summary>释放锁输入。</summary>
public record ReleaseLockDto(string Token);

/// <summary>临界区执行输入。</summary>
public record CriticalDto(int? WorkMs, int? TtlSeconds);
