using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Store.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Store.Queries;

public record GetProductsQuery(string? Category = null) : IRequest<List<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IApplicationDbContext _db;

    public GetProductsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = _db.Products.AsNoTracking().Where(p => p.IsPublished);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(p => p.Category == request.Category);

        return await query
            .OrderBy(p => p.SortOrder)
            .Select(p => new ProductDto(
                p.Id,
                p.Title,
                p.Description,
                p.Price,
                p.ImageUrl,
                p.Category,
                p.InStock,
                p.Quantity,
                p.Rating))
            .ToListAsync(ct);
    }
}
