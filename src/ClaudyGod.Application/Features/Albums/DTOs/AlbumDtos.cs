namespace ClaudyGod.Application.Features.Albums.DTOs;

public record AlbumDto(
    Guid Id,
    string Title,
    string? ImageUrl,
    string? SpotifyUrl,
    string? AppleUrl,
    string? YoutubeUrl,
    string? DeezerUrl,
    string? AmazonUrl,
    int SortOrder,
    DateTime? ReleasedAt
);
