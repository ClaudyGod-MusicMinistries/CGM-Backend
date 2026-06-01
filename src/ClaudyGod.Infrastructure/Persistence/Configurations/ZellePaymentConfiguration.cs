using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class ZellePaymentConfiguration : IEntityTypeConfiguration<ZellePayment>
{
    public void Configure(EntityTypeBuilder<ZellePayment> builder)
    {
        builder.HasKey(z => z.Id);
        builder.Property(z => z.SenderEmail).HasMaxLength(256);
        builder.Property(z => z.SenderPhone).HasMaxLength(30);
        builder.Property(z => z.TransactionId).HasMaxLength(100).IsRequired();
        builder.Property(z => z.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(z => z.Currency).HasMaxLength(10).IsRequired();
        builder.Property(z => z.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(z => z.Purpose).HasMaxLength(500);

        builder.HasQueryFilter(z => !z.IsDeleted);

        builder.HasIndex(z => z.TransactionId).IsUnique();
        builder.HasIndex(z => z.SenderEmail);
        builder.HasIndex(z => z.Status);
        builder.HasIndex(z => new { z.Status, z.CreatedAt });
    }
}
