using System.Collections.Concurrent;
using Demo.Rest.Api.Dtos;
using Demo.Rest.Api.Entities;

namespace Demo.Rest.Api.Application;

/// <summary>订单应用服务。</summary>
public sealed class OrderAppService : IOrderAppService
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Task<List<OrderDto>> GetListAsync()
    {
        var list = _orders.Values
            .OrderBy(o => o.CreatedAt)
            .Select(MapToDto)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<OrderDto?> GetAsync(Guid id)
    {
        if (!_orders.TryGetValue(id, out var entity))
        {
            return Task.FromResult<OrderDto?>(null);
        }

        return Task.FromResult<OrderDto?>(MapToDto(entity));
    }

    public Task<OrderDto> CreateAsync(CreateOrderDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateCreate(input);

        var now = DateTimeOffset.UtcNow;
        var entity = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = input.CustomerName.Trim(),
            Amount = input.Amount,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Pending" : input.Status.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _orders[entity.Id] = entity;
        return Task.FromResult(MapToDto(entity));
    }

    public Task<OrderDto?> UpdateAsync(Guid id, UpdateOrderDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!_orders.TryGetValue(id, out var existing))
        {
            return Task.FromResult<OrderDto?>(null);
        }

        ValidateUpdate(input);

        var updated = new Order
        {
            Id = existing.Id,
            CustomerName = input.CustomerName.Trim(),
            Amount = input.Amount,
            Status = input.Status.Trim(),
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _orders[id] = updated;
        return Task.FromResult<OrderDto?>(MapToDto(updated));
    }

    public Task<OrderDto?> PatchAsync(Guid id, PatchOrderDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!_orders.TryGetValue(id, out var existing))
        {
            return Task.FromResult<OrderDto?>(null);
        }

        if (input.Amount is { } amount && amount <= 0)
        {
            throw new ArgumentException("订单金额必须大于 0", nameof(input.Amount));
        }

        var updated = new Order
        {
            Id = existing.Id,
            CustomerName = string.IsNullOrWhiteSpace(input.CustomerName)
                ? existing.CustomerName
                : input.CustomerName.Trim(),
            Amount = input.Amount ?? existing.Amount,
            Status = string.IsNullOrWhiteSpace(input.Status)
                ? existing.Status
                : input.Status.Trim(),
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _orders[id] = updated;
        return Task.FromResult<OrderDto?>(MapToDto(updated));
    }

    public Task<bool> DeleteAsync(Guid id) =>
        Task.FromResult(_orders.TryRemove(id, out _));

    private static void ValidateCreate(CreateOrderDto input)
    {
        if (string.IsNullOrWhiteSpace(input.CustomerName))
        {
            throw new ArgumentException("客户名称不能为空", nameof(input.CustomerName));
        }

        if (input.Amount <= 0)
        {
            throw new ArgumentException("订单金额必须大于 0", nameof(input.Amount));
        }
    }

    private static void ValidateUpdate(UpdateOrderDto input)
    {
        if (string.IsNullOrWhiteSpace(input.CustomerName))
        {
            throw new ArgumentException("客户名称不能为空", nameof(input.CustomerName));
        }

        if (input.Amount <= 0)
        {
            throw new ArgumentException("订单金额必须大于 0", nameof(input.Amount));
        }

        if (string.IsNullOrWhiteSpace(input.Status))
        {
            throw new ArgumentException("订单状态不能为空", nameof(input.Status));
        }
    }

    private static OrderDto MapToDto(Order entity) => new()
    {
        Id = entity.Id,
        CustomerName = entity.CustomerName,
        Amount = entity.Amount,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
