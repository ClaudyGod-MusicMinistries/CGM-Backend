using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Events.Commands;

public record UpdateEventStatusCommand(Guid EventId, string Status) : IRequest;

public class UpdateEventStatusCommandValidator : AbstractValidator<UpdateEventStatusCommand>
{
    public UpdateEventStatusCommandValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<EventStatus>(s, true, out _))
            .WithMessage("Invalid event status.");
    }
}

public class UpdateEventStatusCommandHandler : IRequestHandler<UpdateEventStatusCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateEventStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateEventStatusCommand request, CancellationToken ct)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == request.EventId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Event not found.");

        var status = Enum.Parse<EventStatus>(request.Status, true);
        switch (status)
        {
            case EventStatus.Cancelled: ev.Cancel(); break;
            case EventStatus.Completed: ev.Complete(); break;
            case EventStatus.Postponed: ev.Postpone(); break;
        }

        await _db.SaveChangesAsync(ct);
    }
}
