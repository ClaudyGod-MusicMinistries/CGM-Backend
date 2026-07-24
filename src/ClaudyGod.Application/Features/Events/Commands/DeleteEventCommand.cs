using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Events.Commands;

public record DeleteEventCommand(Guid EventId) : IRequest;

public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteEventCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteEventCommand request, CancellationToken ct)
    {
        var @event = await _db.Events.FirstOrDefaultAsync(e => e.Id == request.EventId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Event not found.");

        @event.IsDeleted = true;
        @event.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
