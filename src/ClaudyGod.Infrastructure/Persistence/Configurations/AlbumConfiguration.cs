using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(300).IsRequired();
        builder.Property(a => a.ImageUrl).HasMaxLength(500);
        builder.Property(a => a.SpotifyUrl).HasMaxLength(500);
        builder.Property(a => a.AppleUrl).HasMaxLength(500);
        builder.Property(a => a.YoutubeUrl).HasMaxLength(500);
        builder.Property(a => a.DeezerUrl).HasMaxLength(500);
        builder.Property(a => a.AmazonUrl).HasMaxLength(500);

        builder.HasIndex(a => new { a.IsPublished, a.SortOrder });
    }
}
