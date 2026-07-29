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

public record CreateAlbumRequest(
    string Title,
    string? ImageUrl = null,
    string? SpotifyUrl = null,
    string? AppleUrl = null,
    string? YoutubeUrl = null,
    string? DeezerUrl = null,
    string? AmazonUrl = null,
    int SortOrder = 0,
    DateTime? ReleasedAt = null);
