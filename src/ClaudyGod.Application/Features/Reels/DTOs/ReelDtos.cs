namespace ClaudyGod.Application.Features.Reels.DTOs;

public record ReelDto(
    Guid Id,
    string Title,
    string? Description,
    string VideoUrl,
    string? ThumbnailUrl,
    string Category,
    bool IsPublished,
    DateTime? PublishedAt,
    int SortOrder
);
