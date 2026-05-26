using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Albums.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Albums.Queries;

public record GetAlbumsQuery : IRequest<List<AlbumDto>>;

public class GetAlbumsQueryHandler : IRequestHandler<GetAlbumsQuery, List<AlbumDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAlbumsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<AlbumDto>> Handle(GetAlbumsQuery request, CancellationToken ct)
    {
        return await _db.Albums
            .AsNoTracking()
            .Where(a => a.IsPublished)
            .OrderByDescending(a => a.SortOrder)
            .ThenByDescending(a => a.ReleasedAt)
            .Select(a => new AlbumDto(
                a.Id,
                a.Title,
                a.ImageUrl,
                a.SpotifyUrl,
                a.AppleUrl,
                a.YoutubeUrl,
                a.DeezerUrl,
                a.AmazonUrl,
                a.SortOrder,
                a.ReleasedAt))
            .ToListAsync(ct);
    }
}
