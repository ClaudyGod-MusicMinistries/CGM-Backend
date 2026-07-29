using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Rating).HasColumnType("decimal(3,2)");
        builder.Property(p => p.Version).IsRowVersion();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Products_Price_NonNegative", "\"Price\" >= 0");
            t.HasCheckConstraint("CK_Products_Quantity_NonNegative", "\"Quantity\" IS NULL OR \"Quantity\" >= 0");
            t.HasCheckConstraint("CK_Products_Rating_Range", "\"Rating\" IS NULL OR (\"Rating\" >= 0 AND \"Rating\" <= 5)");
        });

        builder.HasIndex(p => new { p.IsPublished, p.SortOrder });
        builder.HasIndex(p => p.Category);
    }
}
