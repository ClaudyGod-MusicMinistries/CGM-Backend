using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Blog.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Blog.Queries;

public record GetBlogPostBySlugQuery(string Slug) : IRequest<BlogPostDetailDto>;

public class GetBlogPostBySlugQueryHandler : IRequestHandler<GetBlogPostBySlugQuery, BlogPostDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetBlogPostBySlugQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<BlogPostDetailDto> Handle(GetBlogPostBySlugQuery request, CancellationToken ct)
    {
        var post = await _db.BlogPosts
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.BlogTag)
            .FirstOrDefaultAsync(p => p.Slug == request.Slug.ToLowerInvariant(), ct)
            ?? throw new Domain.Exceptions.NotFoundException($"Blog post '{request.Slug}' not found.");

        post.IncrementView();
        await _db.SaveChangesAsync(ct);

        return new BlogPostDetailDto(
            post.Id, post.Title, post.Slug, post.Content, post.Excerpt,
            post.FeaturedImagePath, post.Status.ToString(), post.PublishedAt,
            post.AuthorName, post.CategoryId,
            post.Category?.Name, post.PostTags.Select(pt => pt.BlogTag.Name).ToList(),
            post.ViewCount, post.IsFeatured, post.CreatedAt);
    }
}
