namespace ClaudyGod.Domain.Entities;

public class Reel : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string VideoUrl { get; private set; } = string.Empty;
    public string? ThumbnailUrl { get; private set; }
    public string Category { get; private set; } = "featured";
    public bool IsPublished { get; private set; } = true;
    public DateTime? PublishedAt { get; private set; }
    public int SortOrder { get; private set; }

    protected Reel() { }

    public static Reel Create(string title, string videoUrl,
        string category = "featured", string? description = null,
        string? thumbnailUrl = null, bool publish = true, int sortOrder = 0) =>
        new()
        {
            Title = title.Trim(),
            VideoUrl = videoUrl.Trim(),
            Category = category.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            ThumbnailUrl = thumbnailUrl,
            IsPublished = publish,
            PublishedAt = publish ? DateTime.UtcNow : null,
            SortOrder = sortOrder
        };

    public void Publish()
    {
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
    }

    public void Unpublish() => IsPublished = false;
    public void UpdateSortOrder(int order) => SortOrder = order;
    public void SetThumbnail(string url) => ThumbnailUrl = url;
}
