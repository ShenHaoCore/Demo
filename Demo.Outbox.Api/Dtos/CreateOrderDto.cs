namespace Demo.Outbox.Api.Dtos;

/// <summary>创建订单输入。</summary>
public record CreateOrderDto(string Product, int Quantity);
