using Demo.Seckill.Api.Entities;

namespace Demo.Seckill.Api.Dtos;

/// <summary>创建秒杀活动输入。</summary>
public record CreateActivityDto(string Name, int Stock);

/// <summary>抢购输入。</summary>
public record GrabDto(string? UserId);

/// <summary>抢购结果。</summary>
public record GrabResult(GrabStatus Status, Guid? TicketId, int Remaining);
