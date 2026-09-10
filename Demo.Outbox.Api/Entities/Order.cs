namespace Demo.Outbox.Api.Entities;

/// <summary>订单实体。</summary>
public record Order(Guid Id, string Product, int Quantity, DateTimeOffset CreatedAt);
