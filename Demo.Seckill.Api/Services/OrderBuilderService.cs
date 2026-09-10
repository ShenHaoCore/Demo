using Demo.Seckill.Api.Application;

namespace Demo.Seckill.Api.Services;

/// <summary>异步建单后台服务（基础设施）。</summary>
public sealed class OrderBuilderService(ISeckillAppService seckillAppService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var ticket in seckillAppService.Reader.ReadAllAsync(stoppingToken))
        {
            // 模拟异步建单耗时
            await Task.Delay(30, stoppingToken);
            seckillAppService.MarkBuilt(ticket.TicketId, Guid.NewGuid());
        }
    }
}
