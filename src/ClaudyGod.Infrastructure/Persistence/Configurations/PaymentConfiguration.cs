using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.OrderReference).HasMaxLength(100).IsRequired();
        builder.Property(p => p.ConfirmationId).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.UserEmail).HasMaxLength(256);
        builder.Property(p => p.PaymentMethod).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.OrderReference).IsUnique();
        builder.HasIndex(p => p.ConfirmationId);
        builder.HasIndex(p => p.OrderId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.Status, p.CreatedAt });
    }
}
