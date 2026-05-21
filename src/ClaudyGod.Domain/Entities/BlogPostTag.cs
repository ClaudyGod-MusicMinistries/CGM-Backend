namespace ClaudyGod.Domain.Entities;

public class BlogPostTag
{
    public Guid BlogPostId { get; set; }
    public Guid BlogTagId { get; set; }
    public BlogPost BlogPost { get; set; } = null!;
    public BlogTag BlogTag { get; set; } = null!;
}
