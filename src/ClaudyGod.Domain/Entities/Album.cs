namespace ClaudyGod.Domain.Entities;

public class Album : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public string? SpotifyUrl { get; private set; }
    public string? AppleUrl { get; private set; }
    public string? YoutubeUrl { get; private set; }
    public string? DeezerUrl { get; private set; }
    public string? AmazonUrl { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPublished { get; private set; } = true;
    public DateTime? ReleasedAt { get; private set; }

    protected Album() { }

    public static Album Create(
        string title,
        string? imageUrl = null,
        string? spotifyUrl = null,
        string? appleUrl = null,
        string? youtubeUrl = null,
        string? deezerUrl = null,
        string? amazonUrl = null,
        int sortOrder = 0,
        DateTime? releasedAt = null) =>
        new()
        {
            Title = title.Trim(),
            ImageUrl = imageUrl,
            SpotifyUrl = spotifyUrl,
            AppleUrl = appleUrl,
            YoutubeUrl = youtubeUrl,
            DeezerUrl = deezerUrl,
            AmazonUrl = amazonUrl,
            SortOrder = sortOrder,
            ReleasedAt = releasedAt,
            IsPublished = true
        };

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
    public void UpdateSortOrder(int order) => SortOrder = order;
}
