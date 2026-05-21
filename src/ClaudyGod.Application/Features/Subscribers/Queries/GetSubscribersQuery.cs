using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Subscribers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Subscribers.Queries;

public record GetSubscribersQuery(int Page = 1, int PageSize = 20, bool? IsActive = null)
    : IRequest<PaginatedResult<SubscriberDto>>;

public class GetSubscribersQueryHandler : IRequestHandler<GetSubscribersQuery, PaginatedResult<SubscriberDto>>
{
    private readonly IApplicationDbContext _db;

    public GetSubscribersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<SubscriberDto>> Handle(GetSubscribersQuery request, CancellationToken ct)
    {
        var query = _db.Subscribers.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SubscriberDto(s.Id, s.Name, s.Email, s.IsActive, s.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<SubscriberDto>.Create(items, total, request.Page, request.PageSize);
    }
}
