using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Blog.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Blog.Queries;

public record GetBlogTagsQuery : IRequest<List<BlogTagDto>>;

public class GetBlogTagsQueryHandler : IRequestHandler<GetBlogTagsQuery, List<BlogTagDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBlogTagsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<BlogTagDto>> Handle(GetBlogTagsQuery request, CancellationToken ct)
    {
        return await _db.BlogTags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new BlogTagDto(t.Id, t.Name))
            .ToListAsync(ct);
    }
}
