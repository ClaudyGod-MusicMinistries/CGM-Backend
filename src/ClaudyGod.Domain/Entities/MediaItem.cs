using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Domain.Entities;

public class MediaItem : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public MediaType Type { get; private set; }
    public string FilePath { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string? ThumbnailPath { get; private set; }
    public string? ArtistName { get; private set; }
    public string? AlbumName { get; private set; }
    public int? DurationSeconds { get; private set; }
    public bool IsPublished { get; private set; } = false;
    public int ViewCount { get; private set; } = 0;
    public int DownloadCount { get; private set; } = 0;

    protected MediaItem() { }

    public static MediaItem Create(string title, MediaType type, string filePath,
        string fileName, string contentType, long fileSizeBytes,
        string? description = null, string? artistName = null, string? albumName = null) =>
        new()
        {
            Title = title.Trim(),
            Type = type,
            FilePath = filePath,
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            Description = description,
            ArtistName = artistName,
            AlbumName = albumName
        };

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
    public void IncrementView() => ViewCount++;
    public void IncrementDownload() => DownloadCount++;
    public void SetThumbnail(string path) => ThumbnailPath = path;
    public void SetDuration(int seconds) => DurationSeconds = seconds;
}
