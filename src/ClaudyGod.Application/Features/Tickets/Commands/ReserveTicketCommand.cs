using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Tickets.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Tickets.Commands;

public record ReserveTicketCommand(ReserveTicketRequest Request) : IRequest<string>;

public class ReserveTicketCommandValidator : AbstractValidator<ReserveTicketCommand>
{
    public ReserveTicketCommandValidator()
    {
        RuleFor(x => x.Request.EventId).NotEmpty();
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.Quantity).InclusiveBetween(1, 10);
    }
}

public class ReserveTicketCommandHandler : IRequestHandler<ReserveTicketCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;

    public ReserveTicketCommandHandler(IApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<string> Handle(ReserveTicketCommand request, CancellationToken ct)
    {
        var r = request.Request;

        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.Id == r.EventId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Event not found.");

        if (!ev.HasAvailableSeats() || ev.AvailableSeats < r.Quantity)
            throw new Domain.Exceptions.DomainException("Not enough seats available.");

        var confirmationCode = GenerateConfirmationCode();

        var reservation = TicketReservation.Create(ev.Id, r.FirstName, r.LastName,
            r.Email, r.Phone, r.Quantity, confirmationCode);

        ev.IncrementReserved(r.Quantity);
        _db.TicketReservations.Add(reservation);
        await _db.SaveChangesAsync(ct);

        await _email.SendFromTemplateAsync(r.Email, "ticket-confirmation", new Dictionary<string, string>
        {
            ["subject"] = $"Ticket Confirmed – {ev.Title}",
            ["name"] = $"{r.FirstName} {r.LastName}",
            ["eventName"] = ev.Title,
            ["eventDate"] = ev.StartDate.ToString("MMMM dd, yyyy h:mm tt"),
            ["venue"] = ev.Venue ?? "TBD",
            ["quantity"] = r.Quantity.ToString(),
            ["confirmationCode"] = confirmationCode
        }, ct);

        return confirmationCode;
    }

    private static string GenerateConfirmationCode() =>
        $"CGM-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
