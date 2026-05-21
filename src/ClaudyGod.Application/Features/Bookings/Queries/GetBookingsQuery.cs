using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Bookings.DTOs;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Bookings.Queries;

public record GetBookingsQuery(int Page = 1, int PageSize = 20, BookingStatus? Status = null)
    : IRequest<PaginatedResult<BookingDto>>;

public class GetBookingsQueryHandler : IRequestHandler<GetBookingsQuery, PaginatedResult<BookingDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBookingsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<BookingDto>> Handle(GetBookingsQuery request, CancellationToken ct)
    {
        var query = _db.Bookings.AsNoTracking();

        if (request.Status.HasValue)
            query = query.Where(b => b.Status == request.Status.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BookingDto(b.Id, b.FirstName, b.LastName, b.Email, b.Phone,
                b.Organization, b.EventType, b.EventDetails, b.EventDate,
                b.Status.ToString(), b.AdminNotes, b.CreatedAt))
            .ToListAsync(ct);

        return PaginatedResult<BookingDto>.Create(items, total, request.Page, request.PageSize);
    }
}
