namespace ClaudyGod.Domain.Entities;

public class PostLike : BaseEntity
{
    public Guid BlogPostId { get; private set; }
    public BlogPost BlogPost { get; private set; } = null!;
    public string VisitorToken { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    protected PostLike() { }

    public static PostLike Create(Guid blogPostId, string visitorToken) =>
        new()
        {
            BlogPostId = blogPostId,
            VisitorToken = visitorToken.Trim(),
            CreatedAt = DateTime.UtcNow
        };
}
