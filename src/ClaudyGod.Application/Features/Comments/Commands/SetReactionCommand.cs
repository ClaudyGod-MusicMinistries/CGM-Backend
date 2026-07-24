using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Comments.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Commands;

// Exactly one of BlogPostId/CommentId is set — the two public routes that
// create this command (BlogController's post-scoped and comment-scoped
// reaction endpoints) each populate only their own target id.
public record SetReactionCommand(Guid? BlogPostId, Guid? CommentId, string VisitorToken, string Emoji)
    : IRequest<ReactionSummaryDto>;

public class SetReactionCommandValidator : AbstractValidator<SetReactionCommand>
{
    public SetReactionCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.BlogPostId.HasValue ^ x.CommentId.HasValue)
            .WithMessage("Exactly one of BlogPostId or CommentId must be set.");
        RuleFor(x => x.VisitorToken).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Emoji)
            .Must(ReactionEmoji.IsValid)
            .WithMessage($"Emoji must be one of: {string.Join(' ', ReactionEmoji.Allowed)}");
    }
}

public class SetReactionCommandHandler : IRequestHandler<SetReactionCommand, ReactionSummaryDto>
{
    private readonly IApplicationDbContext _db;

    public SetReactionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ReactionSummaryDto> Handle(SetReactionCommand request, CancellationToken ct)
    {
        if (request.BlogPostId.HasValue)
        {
            var postExists = await _db.BlogPosts.AnyAsync(p => p.Id == request.BlogPostId.Value, ct);
            if (!postExists) throw new Domain.Exceptions.NotFoundException("Blog post not found.");
        }
        else
        {
            var commentExists = await _db.Comments.AnyAsync(c => c.Id == request.CommentId!.Value, ct);
            if (!commentExists) throw new Domain.Exceptions.NotFoundException("Comment not found.");
        }

        var existing = await _db.Reactions.FirstOrDefaultAsync(r =>
            r.BlogPostId == request.BlogPostId &&
            r.CommentId == request.CommentId &&
            r.VisitorToken == request.VisitorToken, ct);

        if (existing is not null)
        {
            existing.ChangeEmoji(request.Emoji);
        }
        else
        {
            var reaction = request.BlogPostId.HasValue
                ? Reaction.ForPost(request.BlogPostId.Value, request.VisitorToken, request.Emoji)
                : Reaction.ForComment(request.CommentId!.Value, request.VisitorToken, request.Emoji);
            _db.Reactions.Add(reaction);
        }

        await _db.SaveChangesAsync(ct);

        return await BuildSummary(_db, request.BlogPostId, request.CommentId, request.VisitorToken, ct);
    }

    internal static async Task<ReactionSummaryDto> BuildSummary(
        IApplicationDbContext db, Guid? blogPostId, Guid? commentId, string visitorToken, CancellationToken ct)
    {
        var query = db.Reactions.AsNoTracking().Where(r => r.BlogPostId == blogPostId && r.CommentId == commentId);

        var counts = await query
            .GroupBy(r => r.Emoji)
            .Select(g => new { Emoji = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var yourReaction = await query
            .Where(r => r.VisitorToken == visitorToken)
            .Select(r => r.Emoji)
            .FirstOrDefaultAsync(ct);

        return new ReactionSummaryDto(counts.ToDictionary(c => c.Emoji, c => c.Count), yourReaction);
    }
}
