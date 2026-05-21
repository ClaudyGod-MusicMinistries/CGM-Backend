using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Tickets.DTOs;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Tickets.Queries;

public record GetTicketsQuery(Guid? EventId = null, int Page = 1, int PageSize = 20,
    TicketStatus? Status = null) : IRequest<PaginatedResult<TicketDto>>;

public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, PaginatedResult<TicketDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTicketsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<TicketDto>> Handle(GetTicketsQuery request, CancellationToken ct)
    {
        var query = _db.TicketReservations
            .AsNoTracking()
            .Include(t => t.Event);

        IQueryable<Domain.Entities.TicketReservation> filtered = query;

        if (request.EventId.HasValue)
            filtered = filtered.Where(t => t.EventId == request.EventId.Value);

        if (request.Status.HasValue)
            filtered = filtered.Where(t => t.Status == request.Status.Value);

        var total = await filtered.CountAsync(ct);

        var items = await filtered
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TicketDto(t.Id, t.EventId, t.Event.Title,
                t.AttendeeFirstName, t.AttendeeLastName, t.AttendeeEmail,
                t.Quantity, t.ConfirmationCode, t.Status.ToString(),
                t.CheckedInAt, t.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<TicketDto>.Create(items, total, request.Page, request.PageSize);
    }
}
