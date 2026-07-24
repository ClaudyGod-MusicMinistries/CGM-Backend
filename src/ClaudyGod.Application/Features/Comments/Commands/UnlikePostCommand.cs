using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Commands;

public record UnlikePostCommand(Guid BlogPostId, string VisitorToken) : IRequest<int>;

public class UnlikePostCommandHandler : IRequestHandler<UnlikePostCommand, int>
{
    private readonly IApplicationDbContext _db;

    public UnlikePostCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<int> Handle(UnlikePostCommand request, CancellationToken ct)
    {
        var like = await _db.PostLikes.FirstOrDefaultAsync(
            l => l.BlogPostId == request.BlogPostId && l.VisitorToken == request.VisitorToken, ct);

        if (like is not null)
        {
            _db.PostLikes.Remove(like);
            await _db.SaveChangesAsync(ct);
        }

        return await _db.PostLikes.CountAsync(l => l.BlogPostId == request.BlogPostId, ct);
    }
}
