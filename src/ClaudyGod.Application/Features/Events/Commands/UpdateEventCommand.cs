using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Events.DTOs;
using ClaudyGod.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Events.Commands;

public record UpdateEventCommand(Guid EventId, CreateEventRequest Request) : IRequest;

public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.TotalCapacity).GreaterThan(0);
        RuleFor(x => x.Request.TicketPrice).GreaterThan(0)
            .When(x => !x.Request.IsFree)
            .WithMessage("Ticket price is required for paid events.");
    }
}

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateEventCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateEventCommand request, CancellationToken ct)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == request.EventId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Event not found.");

        var r = request.Request;

        Address? location = null;
        if (r.AddressLine1 is not null && r.City is not null)
            location = new Address(r.AddressLine1, null, r.City, r.State ?? "", r.ZipCode ?? "", r.Country ?? "");

        ev.Update(r.Title, r.StartDate, r.TotalCapacity, r.Description,
            r.Venue, location, r.EndDate, r.IsFree, r.TicketPrice);

        await _db.SaveChangesAsync(ct);
    }
}
