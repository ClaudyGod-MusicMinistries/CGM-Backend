using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.FAQs.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.FAQs.Queries;

public record GetFAQsQuery(string? Category = null) : IRequest<List<FAQDto>>;

public class GetFAQsQueryHandler : IRequestHandler<GetFAQsQuery, List<FAQDto>>
{
    private readonly IApplicationDbContext _db;

    public GetFAQsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<FAQDto>> Handle(GetFAQsQuery request, CancellationToken ct)
    {
        var query = _db.FAQs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(f => f.Category == request.Category);
        }

        var faqs = await query
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Order)
            .Select(f => new FAQDto
            {
                Id = f.Id,
                Question = f.Question,
                Answer = f.Answer,
                Category = f.Category,
                Order = f.Order
            })
            .ToListAsync(ct);

        return faqs;
    }
}
