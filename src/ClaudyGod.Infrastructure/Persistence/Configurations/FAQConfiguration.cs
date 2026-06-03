using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class FAQConfiguration : IEntityTypeConfiguration<FAQ>
{
    public void Configure(EntityTypeBuilder<FAQ> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Question).HasMaxLength(500).IsRequired();
        builder.Property(f => f.Answer).HasMaxLength(5000).IsRequired();
        builder.Property(f => f.Category).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Order).IsRequired();
        builder.Property(f => f.IsPublished).IsRequired();

        builder.HasIndex(f => new { f.Category, f.Order });
        builder.HasIndex(f => f.IsPublished);
    }
}
