using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class PaystackPaymentConfiguration : IEntityTypeConfiguration<PaystackPayment>
{
    public void Configure(EntityTypeBuilder<PaystackPayment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.DonorName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.DonorEmail).HasMaxLength(255).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(10).IsRequired();
        builder.Property(p => p.Reference).HasMaxLength(100).IsRequired();
        builder.Property(p => p.IsVerified).IsRequired();
        builder.Property(p => p.VerifiedAt);
        builder.Property(p => p.Message).HasMaxLength(500);

        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Reference).IsUnique();
        builder.HasIndex(p => p.IsVerified);
        builder.HasIndex(p => p.CreatedAt);
    }
}
