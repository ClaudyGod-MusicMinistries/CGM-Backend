using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Events.DTOs;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Events.Queries;

public record GetEventsQuery(int Page = 1, int PageSize = 10, EventStatus? Status = null)
    : IRequest<PaginatedResult<EventDto>>;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PaginatedResult<EventDto>>
{
    private readonly IApplicationDbContext _db;

    public GetEventsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<EventDto>> Handle(GetEventsQuery request, CancellationToken ct)
    {
        var query = _db.Events.AsNoTracking();

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.StartDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EventDto(e.Id, e.Title, e.Description, e.Venue,
                e.StartDate, e.EndDate, e.TotalCapacity, e.ReservedCount,
                e.AvailableSeats, e.IsFree, e.TicketPrice, e.Status.ToString(),
                e.FlyerImagePath, e.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<EventDto>.Create(items, total, request.Page, request.PageSize);
    }
}
