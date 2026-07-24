using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Comments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Commands;

public record RemoveReactionCommand(Guid? BlogPostId, Guid? CommentId, string VisitorToken)
    : IRequest<ReactionSummaryDto>;

public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand, ReactionSummaryDto>
{
    private readonly IApplicationDbContext _db;

    public RemoveReactionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ReactionSummaryDto> Handle(RemoveReactionCommand request, CancellationToken ct)
    {
        var existing = await _db.Reactions.FirstOrDefaultAsync(r =>
            r.BlogPostId == request.BlogPostId &&
            r.CommentId == request.CommentId &&
            r.VisitorToken == request.VisitorToken, ct);

        if (existing is not null)
        {
            _db.Reactions.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }

        return await SetReactionCommandHandler.BuildSummary(
            _db, request.BlogPostId, request.CommentId, request.VisitorToken, ct);
    }
}
