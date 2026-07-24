using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Blog.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Blog.Queries;

public record GetBlogCategoriesQuery : IRequest<List<BlogCategoryDto>>;

public class GetBlogCategoriesQueryHandler : IRequestHandler<GetBlogCategoriesQuery, List<BlogCategoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBlogCategoriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<BlogCategoryDto>> Handle(GetBlogCategoriesQuery request, CancellationToken ct)
    {
        return await _db.BlogCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new BlogCategoryDto(c.Id, c.Name))
            .ToListAsync(ct);
    }
}
