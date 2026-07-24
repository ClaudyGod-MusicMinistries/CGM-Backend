using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Commands;

public record DeleteCommentCommand(Guid CommentId) : IRequest;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCommentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Comment not found.");

        var now = DateTime.UtcNow;
        comment.IsDeleted = true;
        comment.DeletedAt = now;

        // The FK's DB-level cascade only fires on a real row delete, not this
        // soft-delete flag — without this, a deleted parent's replies would
        // stay visible with no context. Soft-delete them too (one level deep,
        // so this is just direct children, no recursion needed).
        var replies = await _db.Comments
            .Where(c => c.ParentCommentId == request.CommentId)
            .ToListAsync(ct);
        foreach (var reply in replies)
        {
            reply.IsDeleted = true;
            reply.DeletedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
}
