using ClaudyGod.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Subscribers.Commands;

public record DeleteSubscriberCommand(Guid SubscriberId) : IRequest;

public class DeleteSubscriberCommandHandler : IRequestHandler<DeleteSubscriberCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteSubscriberCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteSubscriberCommand request, CancellationToken ct)
    {
        var subscriber = await _db.Subscribers.FirstOrDefaultAsync(s => s.Id == request.SubscriberId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("Subscriber not found.");

        subscriber.IsDeleted = true;
        subscriber.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
