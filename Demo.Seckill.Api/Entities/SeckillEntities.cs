namespace Demo.Seckill.Api.Entities;

public enum GrabStatus
{
    Success,
    SoldOut,
    NotFound
}

public enum OrderStatus
{
    Queued,
    Built
}

/// <summary>秒杀活动实体。</summary>
public sealed class Activity(Guid id, string name, int stock, int remaining, int successCount, DateTimeOffset createdAt)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public int Stock { get; } = stock;
    public int Remaining = remaining;
    public int SuccessCount = successCount;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}

/// <summary>抢购票据 / 异步建单实体。</summary>
public record OrderTicket(
    Guid TicketId,
    Guid? OrderId,
    Guid ActivityId,
    string UserId,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? BuiltAt);
