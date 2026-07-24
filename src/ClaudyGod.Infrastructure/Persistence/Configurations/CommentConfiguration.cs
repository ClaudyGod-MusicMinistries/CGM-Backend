using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.AuthorName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.AuthorEmail).HasMaxLength(320).IsRequired();
        builder.Property(c => c.Content).HasMaxLength(4000).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(c => c.BlogPost)
            .WithMany()
            .HasForeignKey(c => c.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);

        // One level of nesting only (enforced at the command/validation layer,
        // not the schema) — a reply's ParentComment always has a null
        // ParentCommentId of its own. Postgres has no multiple-cascade-path
        // restriction (unlike SQL Server), so this self-referencing cascade
        // coexists fine with the BlogPost cascade above.
        builder.HasOne(c => c.ParentComment)
            .WithMany()
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.BlogPostId, c.Status });
    }
}

public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.VisitorToken).HasMaxLength(100).IsRequired();

        builder.HasOne(l => l.BlogPost)
            .WithMany()
            .HasForeignKey(l => l.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.BlogPostId, l.VisitorToken }).IsUnique();
    }
}
