namespace Demo.Idempotent.Api.Application;

/// <summary>
/// 
/// </summary>
/// <param name="BodyHash"></param>
/// <param name="StatusCode"></param>
/// <param name="ResponseBodyJson"></param>
/// <param name="Location"></param>
public sealed record IdempotencyRecord(string BodyHash, int StatusCode, string ResponseBodyJson, string? Location);

/// <summary>
/// 
/// </summary>
public enum IdempotencyLookupStatus { Miss, Replay, ConflictBody }

/// <summary>
/// 
/// </summary>
/// <param name="Status"></param>
/// <param name="Record"></param>
public readonly record struct IdempotencyLookupResult(IdempotencyLookupStatus Status, IdempotencyRecord? Record);

/// <summary>
/// 
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="bodyHash"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IdempotencyLookupResult> FindAsync(string key, string bodyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="bodyHash"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IdempotencyLookupResult> FindForWriteAsync(string key, string bodyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="bodyHash"></param>
    /// <param name="statusCode"></param>
    /// <param name="responseBodyJson"></param>
    /// <param name="location"></param>
    /// <param name="orderId"></param>
    /// <param name="ttl"></param>
    void AddCompleted(string key, string bodyHash, int statusCode, string responseBodyJson, string? location, Guid orderId, TimeSpan ttl);
}
