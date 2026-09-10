using Demo.Idempotent.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demo.Idempotent.Api.Data;

/// <summary>本模块 DbContext：订单 + 幂等同库，便于单事务提交。</summary>
public sealed class IdempotentDbContext(DbContextOptions<IdempotentDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<IdempotencyEntry> IdempotencyEntries => Set<IdempotencyEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotencyEntry>(e =>
        {
            e.ToTable("idempotency_entries");
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(128);
            e.Property(x => x.BodyHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.ResponseBodyJson).IsRequired();
            e.Property(x => x.Location).HasMaxLength(512);
            e.HasIndex(x => x.ExpiresAt);
            e.HasOne<Order>()
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Product).HasMaxLength(200).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            // 非唯一：TTL 过期后同 Key 可建新单；并发防重靠 idempotency_entries.PK
            e.HasIndex(x => x.IdempotencyKey);
        });
    }
}
