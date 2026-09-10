using Demo.Idempotent.Api.Application;
using Demo.Idempotent.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demo.Idempotent.Api.Data;

/// <summary>
/// 
/// </summary>
/// <param name="db"></param>
public sealed class DbIdempotencyStore(AppDbContext db) : IIdempotencyStore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="bodyHash"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IdempotencyLookupResult> FindAsync(string key, string bodyHash, CancellationToken cancellationToken = default)
    {
        var entry = await db.IdempotencyEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        return Classify(entry, bodyHash);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="bodyHash"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IdempotencyLookupResult> FindForWriteAsync(string key, string bodyHash, CancellationToken cancellationToken = default)
    {
        var entry = await db.IdempotencyEntries.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (entry is null) { return new IdempotencyLookupResult(IdempotencyLookupStatus.Miss, null); }
        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            db.IdempotencyEntries.Remove(entry);
            return new IdempotencyLookupResult(IdempotencyLookupStatus.Miss, null);
        }
        return Classify(entry, bodyHash);
    }

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
    public void AddCompleted(string key, string bodyHash, int statusCode, string responseBodyJson, string? location, Guid orderId, TimeSpan ttl)
    {
        db.IdempotencyEntries.Add(new IdempotencyEntry
        {
            Key = key,
            BodyHash = bodyHash,
            StatusCode = statusCode,
            ResponseBodyJson = responseBodyJson,
            Location = location,
            OrderId = orderId,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl)
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="bodyHash"></param>
    /// <returns></returns>
    private static IdempotencyLookupResult Classify(IdempotencyEntry? entry, string bodyHash)
    {
        if (entry is null || entry.ExpiresAt <= DateTimeOffset.UtcNow) { return new IdempotencyLookupResult(IdempotencyLookupStatus.Miss, null); }
        var record = ToRecord(entry);
        if (!string.Equals(entry.BodyHash, bodyHash, StringComparison.Ordinal)) { return new IdempotencyLookupResult(IdempotencyLookupStatus.ConflictBody, record); }
        return new IdempotencyLookupResult(IdempotencyLookupStatus.Replay, record);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    private static IdempotencyRecord ToRecord(IdempotencyEntry entry) => new(entry.BodyHash, entry.StatusCode, entry.ResponseBodyJson, entry.Location);
}
