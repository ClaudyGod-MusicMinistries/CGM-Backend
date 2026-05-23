using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Tickets.DTOs;
using ClaudyGod.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Application.Features.Tickets.Commands;

public record ReserveTicketCommand(ReserveTicketRequest Request) : IRequest<string>;

public class ReserveTicketCommandValidator : AbstractValidator<ReserveTicketCommand>
{
    public ReserveTicketCommandValidator()
    {
        RuleFor(x => x.Request.EventId)
            .NotEmpty().WithMessage("Please select an event.");
        RuleFor(x => x.Request.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Request.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Request.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(30);
        RuleFor(x => x.Request.Quantity)
            .InclusiveBetween(1, 10).WithMessage("Ticket quantity must be between 1 and 10.");
    }
}

public class ReserveTicketCommandHandler : IRequestHandler<ReserveTicketCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<ReserveTicketCommandHandler> _logger;

    public ReserveTicketCommandHandler(
        IApplicationDbContext db,
        IEmailService email,
        ILogger<ReserveTicketCommandHandler> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<string> Handle(ReserveTicketCommand request, CancellationToken ct)
    {
        var r = request.Request;

        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.Id == r.EventId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Event not found.");

        if (ev.Status == Domain.Enums.EventStatus.Cancelled)
            throw new Domain.Exceptions.DomainException("This event has been cancelled.");

        if (!ev.HasAvailableSeats() || ev.AvailableSeats < r.Quantity)
            throw new Domain.Exceptions.DomainException(
                $"Not enough seats available. Only {ev.AvailableSeats} seat(s) remaining.");

        var confirmationCode = GenerateConfirmationCode();

        var reservation = TicketReservation.Create(ev.Id, r.FirstName, r.LastName,
            r.Email, r.Phone, r.Quantity, confirmationCode);

        ev.IncrementReserved(r.Quantity);
        _db.TicketReservations.Add(reservation);
        await _db.SaveChangesAsync(ct);

        await _email.TrySendFromTemplateAsync(r.Email, "ticket-confirmation", new Dictionary<string, string>
        {
            ["subject"] = $"Ticket Confirmed – {ev.Title}",
            ["name"] = $"{r.FirstName} {r.LastName}",
            ["eventName"] = ev.Title,
            ["eventDate"] = ev.StartDate.ToString("MMMM dd, yyyy h:mm tt"),
            ["venue"] = ev.Venue ?? "TBD",
            ["quantity"] = r.Quantity.ToString(),
            ["confirmationCode"] = confirmationCode
        }, _logger, ct);

        return confirmationCode;
    }

    private static string GenerateConfirmationCode() =>
        $"CGM-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
