using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.AssetKind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.OriginalFileName).HasMaxLength(300).IsRequired();
        builder.Property(s => s.MimeType).HasMaxLength(150).IsRequired();
        builder.Property(s => s.StorageBucket).HasMaxLength(200).IsRequired();
        builder.Property(s => s.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.RequestedBy).HasMaxLength(200).IsRequired();

        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => new { s.RequestedBy, s.Status });
    }
}
