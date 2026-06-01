using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class PrayerRequestConfiguration : IEntityTypeConfiguration<PrayerRequest>
{
    public void Configure(EntityTypeBuilder<PrayerRequest> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Subject).HasMaxLength(300).IsRequired();
        builder.Property(p => p.RequestText).HasMaxLength(5000).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.AdminResponse).HasMaxLength(5000);
        builder.Property(p => p.RespondedBy).HasMaxLength(200);

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Email);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.Status, p.CreatedAt });
        builder.HasIndex(p => p.IsConfidential);
    }
}
