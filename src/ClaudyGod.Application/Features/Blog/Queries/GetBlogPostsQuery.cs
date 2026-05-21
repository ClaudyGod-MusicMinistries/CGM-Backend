using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Blog.DTOs;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Blog.Queries;

public record GetBlogPostsQuery(int Page = 1, int PageSize = 10,
    BlogPostStatus? Status = null, Guid? CategoryId = null, bool FeaturedOnly = false)
    : IRequest<PaginatedResult<BlogPostDto>>;

public class GetBlogPostsQueryHandler : IRequestHandler<GetBlogPostsQuery, PaginatedResult<BlogPostDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBlogPostsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<BlogPostDto>> Handle(GetBlogPostsQuery request, CancellationToken ct)
    {
        var query = _db.BlogPosts
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.BlogTag);

        IQueryable<Domain.Entities.BlogPost> filtered = query;

        if (request.Status.HasValue)
            filtered = filtered.Where(p => p.Status == request.Status.Value);

        if (request.CategoryId.HasValue)
            filtered = filtered.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.FeaturedOnly)
            filtered = filtered.Where(p => p.IsFeatured);

        var total = await filtered.CountAsync(ct);

        var items = await filtered
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new BlogPostDto(
                p.Id, p.Title, p.Slug, p.Excerpt, p.FeaturedImagePath,
                p.Status.ToString(), p.PublishedAt, p.AuthorName,
                p.Category != null ? p.Category.Name : null,
                p.PostTags.Select(pt => pt.BlogTag.Name).ToList(),
                p.ViewCount, p.IsFeatured, p.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<BlogPostDto>.Create(items, total, request.Page, request.PageSize);
    }
}
