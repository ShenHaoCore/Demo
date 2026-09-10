using Demo.Idempotent.Api.Application.Idempotency;
using Demo.Idempotent.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demo.Idempotent.Api.Data;

/// <summary>EF Core 幂等仓储（与 <see cref="IdempotentDbContext"/> 共享同一工作单元）。</summary>
public sealed class IdempotencyRepository(IdempotentDbContext db) : IIdempotencyRepository
{
    public async Task<IdempotencyCheckResult> FindAsync(
        string key,
        string bodyHash,
        CancellationToken cancellationToken = default)
    {
        var entry = await db.IdempotencyEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        return Classify(entry, bodyHash);
    }

    public async Task<IdempotencyCheckResult> FindForUpdateAsync(
        string key,
        string bodyHash,
        CancellationToken cancellationToken = default)
    {
        var entry = await db.IdempotencyEntries
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (entry is null)
        {
            return new IdempotencyCheckResult(IdempotencyCheckStatus.Miss, null);
        }

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            db.IdempotencyEntries.Remove(entry);
            return new IdempotencyCheckResult(IdempotencyCheckStatus.Miss, null);
        }

        return Classify(entry, bodyHash);
    }

    public void InsertCompleted(string key, IdempotencySnapshot snapshot, Guid orderId, TimeSpan ttl)
    {
        db.IdempotencyEntries.Add(new IdempotencyEntry
        {
            Key = key,
            BodyHash = snapshot.BodyHash,
            StatusCode = snapshot.StatusCode,
            ResponseBodyJson = snapshot.ResponseBodyJson,
            Location = snapshot.Location,
            OrderId = orderId,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl)
        });
    }

    private static IdempotencyCheckResult Classify(IdempotencyEntry? entry, string bodyHash)
    {
        if (entry is null || entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return new IdempotencyCheckResult(IdempotencyCheckStatus.Miss, null);
        }

        var snapshot = ToSnapshot(entry);
        if (!string.Equals(entry.BodyHash, bodyHash, StringComparison.Ordinal))
        {
            return new IdempotencyCheckResult(IdempotencyCheckStatus.ConflictBody, snapshot);
        }

        return new IdempotencyCheckResult(IdempotencyCheckStatus.Replay, snapshot);
    }

    private static IdempotencySnapshot ToSnapshot(IdempotencyEntry entry) =>
        new(entry.BodyHash, entry.StatusCode, entry.ResponseBodyJson, entry.Location);
}
