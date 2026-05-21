using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class BlogPost : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? Excerpt { get; private set; }
    public string? FeaturedImagePath { get; private set; }
    public BlogPostStatus Status { get; private set; } = BlogPostStatus.Draft;
    public DateTime? PublishedAt { get; private set; }
    public Guid? CategoryId { get; private set; }
    public BlogCategory? Category { get; private set; }
    public ICollection<BlogPostTag> PostTags { get; private set; } = [];
    public int ViewCount { get; private set; } = 0;
    public bool IsFeatured { get; private set; } = false;
    public string? AuthorName { get; private set; }

    protected BlogPost() { }

    public static BlogPost Create(string title, string slug, string content,
        string? excerpt = null, string? authorName = null, Guid? categoryId = null) =>
        new()
        {
            Title = title.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            Content = content,
            Excerpt = excerpt,
            AuthorName = authorName,
            CategoryId = categoryId
        };

    public void Publish()
    {
        Status = BlogPostStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void Retract() => Status = BlogPostStatus.Draft;
    public void Archive() => Status = BlogPostStatus.Archived;
    public void IncrementView() => ViewCount++;
    public void SetFeatured(bool featured) => IsFeatured = featured;
    public void SetFeaturedImage(string path) => FeaturedImagePath = path;

    public void Update(string title, string slug, string content, string? excerpt,
        string? authorName, Guid? categoryId)
    {
        Title = title.Trim();
        Slug = slug.ToLowerInvariant().Trim();
        Content = content;
        Excerpt = excerpt;
        AuthorName = authorName;
        CategoryId = categoryId;
    }
}
