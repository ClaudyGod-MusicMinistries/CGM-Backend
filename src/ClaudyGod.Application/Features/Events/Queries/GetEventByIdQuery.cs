using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Events.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Events.Queries;

public record GetEventByIdQuery(Guid EventId) : IRequest<EventDetailDto>;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetEventByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<EventDetailDto> Handle(GetEventByIdQuery request, CancellationToken ct)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EventId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Event not found.");

        return new EventDetailDto(ev.Id, ev.Title, ev.Description, ev.Venue,
            ev.StartDate, ev.EndDate, ev.TotalCapacity, ev.ReservedCount,
            ev.AvailableSeats, ev.IsFree, ev.TicketPrice, ev.Status.ToString(),
            ev.FlyerImagePath, ev.Location?.City, ev.Location?.State,
            ev.Location?.Country, ev.CreatedAt);
    }
}
