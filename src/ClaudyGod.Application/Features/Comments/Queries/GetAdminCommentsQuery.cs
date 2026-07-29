using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Comments.DTOs;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Comments.Queries;

public record GetAdminCommentsQuery(int Page = 1, int PageSize = 20, CommentStatus? Status = null)
    : IRequest<PaginatedResult<AdminCommentDto>>;

public class GetAdminCommentsQueryHandler
    : IRequestHandler<GetAdminCommentsQuery, PaginatedResult<AdminCommentDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAdminCommentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<AdminCommentDto>> Handle(GetAdminCommentsQuery request, CancellationToken ct)
    {
        var query = _db.Comments.AsNoTracking().Include(c => c.BlogPost).AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new AdminCommentDto(
                c.Id,
                c.BlogPostId,
                c.BlogPost.Title,
                c.BlogPost.Slug,
                c.ParentCommentId,
                c.AuthorName,
                c.AuthorEmail,
                c.Content,
                c.Status.ToString(),
                c.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<AdminCommentDto>.Create(items, total, request.Page, request.PageSize);
    }
}
