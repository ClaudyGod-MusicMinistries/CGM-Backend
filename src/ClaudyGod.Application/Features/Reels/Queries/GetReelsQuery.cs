using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Reels.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Reels.Queries;

public record GetReelsQuery(
    string? Category = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<List<ReelDto>>;

public class GetReelsQueryHandler : IRequestHandler<GetReelsQuery, List<ReelDto>>
{
    private readonly IApplicationDbContext _db;

    public GetReelsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ReelDto>> Handle(GetReelsQuery request, CancellationToken ct)
    {
        var query = _db.Reels
            .AsNoTracking()
            .Where(r => r.IsPublished);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(r => r.Category == request.Category.Trim().ToLowerInvariant());

        return await query
            .OrderByDescending(r => r.SortOrder)
            .ThenByDescending(r => r.PublishedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReelDto(
                r.Id,
                r.Title,
                r.Description,
                r.VideoUrl,
                r.ThumbnailUrl,
                r.Category,
                r.IsPublished,
                r.PublishedAt,
                r.SortOrder))
            .ToListAsync(ct);
    }
}
