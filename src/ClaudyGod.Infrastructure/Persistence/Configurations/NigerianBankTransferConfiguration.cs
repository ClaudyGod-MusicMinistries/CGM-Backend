using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class NigerianBankTransferConfiguration : IEntityTypeConfiguration<NigerianBankTransfer>
{
    public void Configure(EntityTypeBuilder<NigerianBankTransfer> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Reference).HasMaxLength(100).IsRequired();
        builder.Property(n => n.SenderName).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(n => n.Currency).HasMaxLength(10).IsRequired();
        builder.Property(n => n.SlipFilePath).HasMaxLength(500).IsRequired();
        builder.Property(n => n.SlipContentType).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(n => n.RejectionReason).HasMaxLength(500);

        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.HasIndex(n => n.Reference).IsUnique();
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.CreatedAt);
    }
}
