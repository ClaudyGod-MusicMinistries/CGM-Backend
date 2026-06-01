using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).HasMaxLength(300).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(m => m.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(m => m.FileName).HasMaxLength(300).IsRequired();
        builder.Property(m => m.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(m => m.ThumbnailPath).HasMaxLength(500);
        builder.Property(m => m.ArtistName).HasMaxLength(200);
        builder.Property(m => m.AlbumName).HasMaxLength(200);

        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.HasIndex(m => m.Type);
        builder.HasIndex(m => m.IsPublished);
        builder.HasIndex(m => new { m.Type, m.IsPublished });
    }
}
