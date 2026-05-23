using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Bookings.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClaudyGod.Application.Features.Bookings.Commands;

public record CreateBookingCommand(CreateBookingRequest Request) : IRequest<Guid>;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
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
        RuleFor(x => x.Request.Organization)
            .NotEmpty().WithMessage("Organization name is required.")
            .MaximumLength(200);
        RuleFor(x => x.Request.OrgType)
            .NotEmpty().WithMessage("Organization type is required.");
        RuleFor(x => x.Request.EventType)
            .NotEmpty().WithMessage("Event type is required.");
        RuleFor(x => x.Request.EventDetails)
            .NotEmpty().WithMessage("Event details are required.")
            .MinimumLength(10).WithMessage("Please describe your event in more detail (minimum 10 characters).")
            .MaximumLength(2000);
        RuleFor(x => x.Request.EventDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future.");
        RuleFor(x => x.Request.AddressLine1)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(200);
        RuleFor(x => x.Request.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100);
        RuleFor(x => x.Request.State)
            .NotEmpty().WithMessage("State / Province is required.")
            .MaximumLength(100);
        RuleFor(x => x.Request.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100);
        RuleFor(x => x.Request.AgreeTerms)
            .Equal(true).WithMessage("You must agree to the terms and conditions.");
    }
}

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<CreateBookingCommandHandler> _logger;

    public CreateBookingCommandHandler(
        IApplicationDbContext db,
        IEmailService email,
        ILogger<CreateBookingCommandHandler> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken ct)
    {
        var r = request.Request;
        var address = new Address(r.AddressLine1, r.AddressLine2, r.City, r.State, r.ZipCode ?? "", r.Country);

        var booking = Booking.Create(r.FirstName, r.LastName, r.Email, r.Phone,
            r.CountryCode, r.Organization, r.OrgType, r.EventType, r.EventDetails,
            r.EventDate, address);

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);

        await _email.TrySendFromTemplateAsync(r.Email, "booking-confirmation", new Dictionary<string, string>
        {
            ["subject"] = "Booking Request Received – ClaudyGod Ministry",
            ["name"] = $"{r.FirstName} {r.LastName}",
            ["eventType"] = r.EventType,
            ["eventDate"] = r.EventDate.ToString("MMMM dd, yyyy"),
            ["organization"] = r.Organization,
            ["bookingId"] = booking.Id.ToString()
        }, _logger, ct);

        return booking.Id;
    }
}
