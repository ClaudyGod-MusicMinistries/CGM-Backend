using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Bookings.Commands;

public record DeleteBookingCommand(Guid BookingId) : IRequest;

public class DeleteBookingCommandHandler : IRequestHandler<DeleteBookingCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteBookingCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteBookingCommand request, CancellationToken ct)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == request.BookingId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Booking not found.");

        booking.IsDeleted = true;
        booking.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
