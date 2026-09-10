using System.Text.Json;
using Demo.Idempotent.Api.Data;
using Demo.Idempotent.Api.Dtos;
using Demo.Idempotent.Api.Entities;
using Demo.Idempotent.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Demo.Idempotent.Api.Application;

/// <summary>
/// 订单应用服务：幂等与下单的唯一决策点；写路径单事务单次 SaveChanges。
/// </summary>
public sealed class OrderAppService(
    AppDbContext db,
    IIdempotencyStore idempotencyStore,
    IOptions<IdempotencyOptions> options) : IOrderAppService
{
    private const int CreatedStatusCode = 201;
    private const string ConflictBodyMessage = "同一 Idempotency-Key 已用于不同请求体，拒绝处理";
    private const string ConflictRaceMessage = "同一 Idempotency-Key 并发冲突，请重试以回放结果";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CreateOrderOutcome> CreateAsync(
        CreateOrderDto input,
        string idempotencyKey,
        string bodyHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.Product) || input.Quantity <= 0)
        {
            return new CreateOrderInvalid("请提供有效的 product 与 quantity（>0）");
        }

        var existing = await idempotencyStore.FindAsync(idempotencyKey, bodyHash, cancellationToken);
        if (TryMapLookup(existing, out var fastPath))
        {
            return fastPath;
        }

        var ttl = options.Value.Ttl;
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var confirmed = await idempotencyStore.FindForWriteAsync(idempotencyKey, bodyHash, cancellationToken);
            if (TryMapLookup(confirmed, out var raced))
            {
                await tx.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();
                return raced;
            }

            var entity = new Order
            {
                Id = Guid.NewGuid(),
                Product = input.Product.Trim(),
                Quantity = input.Quantity,
                CreatedAt = DateTimeOffset.UtcNow,
                IdempotencyKey = idempotencyKey
            };

            var dto = new OrderDto
            {
                Id = entity.Id,
                Product = entity.Product,
                Quantity = entity.Quantity,
                CreatedAt = entity.CreatedAt,
                IdempotencyKey = entity.IdempotencyKey
            };

            var location = $"/api/orders/{dto.Id}";
            var responseJson = JsonSerializer.Serialize(dto, JsonOptions);

            db.Orders.Add(entity);
            idempotencyStore.AddCompleted(
                idempotencyKey,
                bodyHash,
                CreatedStatusCode,
                responseJson,
                location,
                entity.Id,
                ttl);

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new CreateOrderSuccess(dto, location);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();

            var again = await idempotencyStore.FindAsync(idempotencyKey, bodyHash, cancellationToken);
            return TryMapLookup(again, out var mapped)
                ? mapped
                : new CreateOrderConflict(ConflictRaceMessage);
        }
    }

    private static bool TryMapLookup(IdempotencyLookupResult lookup, out CreateOrderOutcome outcome)
    {
        switch (lookup.Status)
        {
            case IdempotencyLookupStatus.Replay:
                outcome = new CreateOrderReplay(lookup.Record!);
                return true;
            case IdempotencyLookupStatus.ConflictBody:
                outcome = new CreateOrderConflict(ConflictBodyMessage);
                return true;
            default:
                outcome = null!;
                return false;
        }
    }
}
