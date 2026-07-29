using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Comments.DTOs;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Queries;

public record GetCommentsForPostQuery(Guid BlogPostId) : IRequest<List<CommentDto>>;

public class GetCommentsForPostQueryHandler : IRequestHandler<GetCommentsForPostQuery, List<CommentDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCommentsForPostQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CommentDto>> Handle(GetCommentsForPostQuery request, CancellationToken ct)
    {
        return await _db.Comments
            .AsNoTracking()
            .Where(c => c.BlogPostId == request.BlogPostId && c.Status == CommentStatus.Approved)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.AuthorName, c.Content, c.ParentCommentId, c.CreatedAt))
            .ToListAsync(ct);
    }
}
