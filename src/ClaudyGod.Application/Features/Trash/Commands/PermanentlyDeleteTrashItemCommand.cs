using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Trash.Commands;

public record PermanentlyDeleteTrashItemCommand(TrashEntityType EntityType, Guid Id) : IRequest;

public class PermanentlyDeleteTrashItemCommandHandler : IRequestHandler<PermanentlyDeleteTrashItemCommand>
{
    private readonly IApplicationDbContext _db;

    public PermanentlyDeleteTrashItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(PermanentlyDeleteTrashItemCommand request, CancellationToken ct)
    {
        switch (request.EntityType)
        {
            case TrashEntityType.Album:
                await RemoveAsync(_db.Albums, request.Id, ct);
                break;
            case TrashEntityType.Product:
                await RemoveAsync(_db.Products, request.Id, ct);
                break;
            case TrashEntityType.MediaItem:
                await RemoveAsync(_db.MediaItems, request.Id, ct);
                break;
            case TrashEntityType.FAQ:
                await RemoveAsync(_db.FAQs, request.Id, ct);
                break;
            case TrashEntityType.Event:
                await RemoveAsync(_db.Events, request.Id, ct);
                break;
            case TrashEntityType.BlogPost:
                await RemoveAsync(_db.BlogPosts, request.Id, ct);
                break;
            case TrashEntityType.Booking:
                await RemoveAsync(_db.Bookings, request.Id, ct);
                break;
            case TrashEntityType.ContactMessage:
                await RemoveAsync(_db.ContactMessages, request.Id, ct);
                break;
            case TrashEntityType.Volunteer:
                await RemoveAsync(_db.Volunteers, request.Id, ct);
                break;
            case TrashEntityType.PrayerRequest:
                await RemoveAsync(_db.PrayerRequests, request.Id, ct);
                break;
            case TrashEntityType.TicketReservation:
                await RemoveAsync(_db.TicketReservations, request.Id, ct);
                break;
            case TrashEntityType.Subscriber:
                await RemoveAsync(_db.Subscribers, request.Id, ct);
                break;
            case TrashEntityType.Comment:
                await RemoveAsync(_db.Comments, request.Id, ct);
                break;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static async Task RemoveAsync<TEntity>(DbSet<TEntity> set, Guid id, CancellationToken ct)
        where TEntity : Domain.Entities.AuditableEntity
    {
        var entity = await set.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Trash item not found.");

        set.Remove(entity);
    }
}
