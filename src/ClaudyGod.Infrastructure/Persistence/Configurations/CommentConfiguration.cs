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

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.VisitorToken).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Emoji).HasMaxLength(16).IsRequired();

        builder.HasOne(r => r.BlogPost)
            .WithMany()
            .HasForeignKey(r => r.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Comment)
            .WithMany()
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // A plain composite unique index on (BlogPostId, CommentId, VisitorToken)
        // would NOT actually prevent duplicates here — Postgres (like the SQL
        // standard) treats two NULLs as distinct for uniqueness purposes, and
        // exactly one of BlogPostId/CommentId is always NULL on every row. Two
        // filtered/partial indexes instead, one per target type, each only
        // covering rows where that target's column is actually set.
        builder.HasIndex(r => new { r.BlogPostId, r.VisitorToken })
            .IsUnique()
            .HasFilter("\"BlogPostId\" IS NOT NULL");

        builder.HasIndex(r => new { r.CommentId, r.VisitorToken })
            .IsUnique()
            .HasFilter("\"CommentId\" IS NOT NULL");
    }
}
