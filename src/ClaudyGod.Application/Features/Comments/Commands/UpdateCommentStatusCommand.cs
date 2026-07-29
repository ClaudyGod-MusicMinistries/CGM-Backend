using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Commands;

public record UpdateCommentStatusCommand(Guid CommentId, CommentStatus Status) : IRequest;

public class UpdateCommentStatusCommandHandler : IRequestHandler<UpdateCommentStatusCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateCommentStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateCommentStatusCommand request, CancellationToken ct)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Comment not found.");

        switch (request.Status)
        {
            case CommentStatus.Approved:
                comment.Approve();
                break;
            case CommentStatus.Rejected:
                comment.Reject();
                break;
        }

        await _db.SaveChangesAsync(ct);
    }
}
