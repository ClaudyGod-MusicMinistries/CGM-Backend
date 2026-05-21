using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Slug).HasMaxLength(500).IsRequired();
        builder.HasIndex(b => b.Slug).IsUnique();
        builder.Property(b => b.Content).IsRequired();
        builder.Property(b => b.Excerpt).HasMaxLength(1000);
        builder.Property(b => b.AuthorName).HasMaxLength(200);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(b => b.PostTags)
            .WithOne(pt => pt.BlogPost)
            .HasForeignKey(pt => pt.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BlogCategoryConfiguration : IEntityTypeConfiguration<BlogCategory>
{
    public void Configure(EntityTypeBuilder<BlogCategory> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();
    }
}

public class BlogTagConfiguration : IEntityTypeConfiguration<BlogTag>
{
    public void Configure(EntityTypeBuilder<BlogTag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class BlogPostTagConfiguration : IEntityTypeConfiguration<BlogPostTag>
{
    public void Configure(EntityTypeBuilder<BlogPostTag> builder)
    {
        builder.HasKey(pt => new { pt.BlogPostId, pt.BlogTagId });

        builder.HasOne(pt => pt.BlogPost)
            .WithMany(b => b.PostTags)
            .HasForeignKey(pt => pt.BlogPostId);

        builder.HasOne(pt => pt.BlogTag)
            .WithMany()
            .HasForeignKey(pt => pt.BlogTagId);
    }
}
