using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Events.DTOs;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Events.Commands;

public record CreateEventCommand(CreateEventRequest Request) : IRequest<Guid>;

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.StartDate).GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future.");
        RuleFor(x => x.Request.TotalCapacity).GreaterThan(0);
        RuleFor(x => x.Request.TicketPrice).GreaterThan(0)
            .When(x => !x.Request.IsFree)
            .WithMessage("Ticket price is required for paid events.");
    }
}

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateEventCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken ct)
    {
        var r = request.Request;

        Address? location = null;
        if (r.AddressLine1 is not null && r.City is not null)
            location = new Address(r.AddressLine1, null, r.City, r.State ?? "", r.ZipCode ?? "", r.Country ?? "");

        var ev = Event.Create(r.Title, r.StartDate, r.TotalCapacity, r.Description,
            r.Venue, location, r.EndDate, r.IsFree, r.TicketPrice);

        _db.Events.Add(ev);
        await _db.SaveChangesAsync(ct);

        return ev.Id;
    }
}
