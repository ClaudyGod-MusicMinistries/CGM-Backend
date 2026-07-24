using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Tickets.Commands;

public record DeleteTicketCommand(Guid TicketId) : IRequest;

public class DeleteTicketCommandHandler : IRequestHandler<DeleteTicketCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteTicketCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteTicketCommand request, CancellationToken ct)
    {
        var ticket = await _db.TicketReservations.FirstOrDefaultAsync(t => t.Id == request.TicketId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Ticket reservation not found.");

        ticket.IsDeleted = true;
        ticket.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
