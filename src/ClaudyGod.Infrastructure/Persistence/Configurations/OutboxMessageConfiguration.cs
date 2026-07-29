using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(4000);
        builder.Property(x => x.LockOwner).HasMaxLength(128);
        builder.HasIndex(x => new { x.ProcessedAt, x.AvailableAt });
        builder.HasIndex(x => x.LockedUntil);
        builder.Property(x => x.Version).IsRowVersion();
    }
}
