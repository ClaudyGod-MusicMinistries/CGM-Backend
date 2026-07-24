using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Comments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Queries;

public record GetLikeStatusQuery(Guid BlogPostId, string? VisitorToken) : IRequest<LikeStatusDto>;

public class GetLikeStatusQueryHandler : IRequestHandler<GetLikeStatusQuery, LikeStatusDto>
{
    private readonly IApplicationDbContext _db;

    public GetLikeStatusQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<LikeStatusDto> Handle(GetLikeStatusQuery request, CancellationToken ct)
    {
        var count = await _db.PostLikes.CountAsync(l => l.BlogPostId == request.BlogPostId, ct);

        var likedByYou = !string.IsNullOrEmpty(request.VisitorToken) &&
            await _db.PostLikes.AnyAsync(
                l => l.BlogPostId == request.BlogPostId && l.VisitorToken == request.VisitorToken, ct);

        return new LikeStatusDto(count, likedByYou);
    }
}
