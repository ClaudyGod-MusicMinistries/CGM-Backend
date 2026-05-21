using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Bookings.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Bookings.Commands;

public record CreateBookingCommand(CreateBookingRequest Request) : IRequest<Guid>;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.Organization).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.OrgType).NotEmpty();
        RuleFor(x => x.Request.EventType).NotEmpty();
        RuleFor(x => x.Request.EventDetails).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Request.EventDate).GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future.");
        RuleFor(x => x.Request.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.AgreeTerms).Equal(true).WithMessage("You must agree to the terms.");
    }
}

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;

    public CreateBookingCommandHandler(IApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken ct)
    {
        var r = request.Request;
        var address = new Address(r.AddressLine1, r.AddressLine2, r.City, r.State, r.ZipCode, r.Country);

        var booking = Booking.Create(r.FirstName, r.LastName, r.Email, r.Phone,
            r.CountryCode, r.Organization, r.OrgType, r.EventType, r.EventDetails,
            r.EventDate, address);

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);

        await _email.SendFromTemplateAsync(r.Email, "booking-confirmation", new Dictionary<string, string>
        {
            ["subject"] = "Booking Request Received – ClaudyGod Ministry",
            ["name"] = $"{r.FirstName} {r.LastName}",
            ["eventType"] = r.EventType,
            ["eventDate"] = r.EventDate.ToString("MMMM dd, yyyy"),
            ["organization"] = r.Organization,
            ["bookingId"] = booking.Id.ToString()
        }, ct);

        return booking.Id;
    }
}
