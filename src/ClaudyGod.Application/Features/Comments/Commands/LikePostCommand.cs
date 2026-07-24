using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Commands;

public record LikePostCommand(Guid BlogPostId, string VisitorToken) : IRequest<int>;

public class LikePostCommandValidator : AbstractValidator<LikePostCommand>
{
    public LikePostCommandValidator()
    {
        RuleFor(x => x.VisitorToken).NotEmpty().MaximumLength(100);
    }
}

public class LikePostCommandHandler : IRequestHandler<LikePostCommand, int>
{
    private readonly IApplicationDbContext _db;

    public LikePostCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<int> Handle(LikePostCommand request, CancellationToken ct)
    {
        var postExists = await _db.BlogPosts.AnyAsync(p => p.Id == request.BlogPostId, ct);
        if (!postExists) throw new Domain.Exceptions.NotFoundException("Blog post not found.");

        // Idempotent — liking twice from the same browser just returns the
        // current count rather than erroring, matching the unique index's
        // own semantics (nothing to add if it's already there).
        var alreadyLiked = await _db.PostLikes.AnyAsync(
            l => l.BlogPostId == request.BlogPostId && l.VisitorToken == request.VisitorToken, ct);

        if (!alreadyLiked)
        {
            _db.PostLikes.Add(PostLike.Create(request.BlogPostId, request.VisitorToken));
            await _db.SaveChangesAsync(ct);
        }

        return await _db.PostLikes.CountAsync(l => l.BlogPostId == request.BlogPostId, ct);
    }
}
