using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Media.DTOs;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Media.Queries;

public record GetMediaQuery(int Page = 1, int PageSize = 20,
    MediaType? Type = null, bool? IsPublished = null)
    : IRequest<PaginatedResult<MediaItemDto>>;

public class GetMediaQueryHandler : IRequestHandler<GetMediaQuery, PaginatedResult<MediaItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    public GetMediaQueryHandler(IApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<PaginatedResult<MediaItemDto>> Handle(GetMediaQuery request, CancellationToken ct)
    {
        var query = _db.MediaItems.AsNoTracking();

        if (request.Type.HasValue)
            query = query.Where(m => m.Type == request.Type.Value);

        if (request.IsPublished.HasValue)
            query = query.Where(m => m.IsPublished == request.IsPublished.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(m => new MediaItemDto(
            m.Id, m.Title, m.Description, m.Type.ToString(),
            m.FileName, m.ContentType, m.FileSizeBytes,
            _storage.GetPublicUrl(m.FilePath),
            m.ThumbnailPath is not null ? _storage.GetPublicUrl(m.ThumbnailPath) : null,
            m.ArtistName, m.AlbumName, m.DurationSeconds,
            m.IsPublished, m.ViewCount, m.DownloadCount, m.CreatedAt)).ToList();

        return PaginatedResult<MediaItemDto>.Create(dtos, total, request.Page, request.PageSize);
    }
}
