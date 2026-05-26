using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class ReelConfiguration : IEntityTypeConfiguration<Reel>
{
    public void Configure(EntityTypeBuilder<Reel> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.Property(r => r.VideoUrl).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ThumbnailUrl).HasMaxLength(500);
        builder.Property(r => r.Category).HasMaxLength(50).IsRequired();

        builder.HasIndex(r => r.Category);
        builder.HasIndex(r => new { r.Category, r.IsPublished, r.SortOrder });
    }
}
