using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class Comment : AuditableEntity
{
    public Guid BlogPostId { get; private set; }
    public BlogPost BlogPost { get; private set; } = null!;
    public Guid? ParentCommentId { get; private set; }
    public Comment? ParentComment { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string AuthorEmail { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public CommentStatus Status { get; private set; } = CommentStatus.Pending;

    protected Comment() { }

    public static Comment Create(
        Guid blogPostId, string authorName, string authorEmail, string content, Guid? parentCommentId = null) =>
        new()
        {
            BlogPostId = blogPostId,
            AuthorName = authorName.Trim(),
            AuthorEmail = authorEmail.Trim().ToLowerInvariant(),
            Content = content.Trim(),
            ParentCommentId = parentCommentId,
            Status = CommentStatus.Pending
        };

    public void Approve() => Status = CommentStatus.Approved;
    public void Reject() => Status = CommentStatus.Rejected;
}
