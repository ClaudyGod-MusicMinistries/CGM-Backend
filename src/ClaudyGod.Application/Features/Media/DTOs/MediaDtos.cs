using ClaudyGod.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace ClaudyGod.Application.Features.Media.DTOs;

public record UploadMediaRequest(
    IFormFile File,
    string Title,
    MediaType Type,
    string? Description,
    string? ArtistName,
    string? AlbumName);

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
