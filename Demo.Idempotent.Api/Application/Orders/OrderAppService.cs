using System.Text.Json;
using Demo.Idempotent.Api.Application.Idempotency;
using Demo.Idempotent.Api.Data;
using Demo.Idempotent.Api.Dtos;
using Demo.Idempotent.Api.Entities;
using Demo.Idempotent.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Demo.Idempotent.Api.Application.Orders;

/// <summary>
/// 订单应用服务（ABP 风格：AppService 编排，单次 SaveChanges 作为工作单元）。
/// </summary>
public sealed class OrderAppService(
    IdempotentDbContext db,
    IIdempotencyRepository idempotencyRepository,
    IOptions<IdempotencyOptions> options) : IOrderAppService
{
    private const int CreatedStatusCode = 201;
    private const string ConflictBodyMessage = "同一 Idempotency-Key 已用于不同请求体，拒绝处理";
    private const string ConflictRaceMessage = "同一 Idempotency-Key 并发冲突，请重试以回放结果";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CreateOrderResult> CreateAsync(
        CreateOrderDto input,
        string idempotencyKey,
        string bodyHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.Product) || input.Quantity <= 0)
        {
            return new CreateOrderInvalidResult("请提供有效的 product 与 quantity（>0）");
        }

        // 快路径：已完成则直接回放/冲突（无写库）
        if (MapCheck(await idempotencyRepository.FindAsync(idempotencyKey, bodyHash, cancellationToken))
            is { } existing)
        {
            return existing;
        }

        // 工作单元内再确认（含过期清理），与下单一并 SaveChanges
        if (MapCheck(await idempotencyRepository.FindForUpdateAsync(idempotencyKey, bodyHash, cancellationToken))
            is { } raced)
        {
            return raced;
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Product = input.Product.Trim(),
            Quantity = input.Quantity,
            CreatedAt = DateTimeOffset.UtcNow,
            IdempotencyKey = idempotencyKey
        };
        var dto = ToDto(order);
        var location = $"/api/orders/{dto.Id}";
        var snapshot = new IdempotencySnapshot(
            bodyHash,
            CreatedStatusCode,
            JsonSerializer.Serialize(dto, JsonOptions),
            location);

        db.Orders.Add(order);
        idempotencyRepository.InsertCompleted(idempotencyKey, snapshot, order.Id, options.Value.Ttl);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new CreateOrderSuccessResult(dto, location);
        }
        catch (DbUpdateException)
        {
            // 并发唯一约束：清跟踪后回查回放
            db.ChangeTracker.Clear();
            return MapCheck(await idempotencyRepository.FindAsync(idempotencyKey, bodyHash, cancellationToken))
                   ?? new CreateOrderConflictResult(ConflictRaceMessage);
        }
    }

    private static CreateOrderResult? MapCheck(IdempotencyCheckResult check) =>
        check.Status switch
        {
            IdempotencyCheckStatus.Replay => new CreateOrderReplayResult(check.Snapshot!),
            IdempotencyCheckStatus.ConflictBody => new CreateOrderConflictResult(ConflictBodyMessage),
            _ => null
        };

    private static OrderDto ToDto(Order order) => new()
    {
        Id = order.Id,
        Product = order.Product,
        Quantity = order.Quantity,
        CreatedAt = order.CreatedAt,
        IdempotencyKey = order.IdempotencyKey
    };
}
