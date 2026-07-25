using ClaudyGod.Domain.Enums;

namespace ClaudyGod.Application.Features.Media.DTOs;

/// <summary>
/// Replaces the old multipart UploadMediaRequest — the file bytes already
/// landed in S3 during the StorageController confirm step (see
/// CreateMediaFromUploadCommand); this only needs the session id plus the
/// content metadata that isn't derivable from the file itself.
/// </summary>
public record CreateMediaFromUploadRequest(
    Guid SessionId,
    string Title,
    MediaType Type,
    string? Description,
    string? ArtistName,
    string? AlbumName);

public record CreateMediaLinkRequest(
    string Title,
    MediaType Type,
    string ExternalUrl,
    string? ThumbnailUrl = null,
    string? Description = null);

public record MediaItemDto(
    Guid Id,
    string Title,
    string? Description,
    string Type,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string PublicUrl,
    string? ThumbnailPath,
    string? ArtistName,
    string? AlbumName,
    int? DurationSeconds,
    bool IsPublished,
    int ViewCount,
    int DownloadCount,
    DateTime CreatedAt);
