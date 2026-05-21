using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Bookings.Commands;

public record UpdateBookingStatusCommand(Guid BookingId, string Status, string? AdminNotes) : IRequest;

public class UpdateBookingStatusCommandValidator : AbstractValidator<UpdateBookingStatusCommand>
{
    public UpdateBookingStatusCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => Enum.TryParse<BookingStatus>(s, true, out _))
            .WithMessage("Invalid booking status.");
    }
}

public class UpdateBookingStatusCommandHandler : IRequestHandler<UpdateBookingStatusCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateBookingStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateBookingStatusCommand request, CancellationToken ct)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == request.BookingId, ct)
            ?? throw new Domain.Exceptions.NotFoundException($"Booking {request.BookingId} not found.");

        var status = Enum.Parse<BookingStatus>(request.Status, true);
        booking.UpdateStatus(status, request.AdminNotes);

        await _db.SaveChangesAsync(ct);
    }
}
