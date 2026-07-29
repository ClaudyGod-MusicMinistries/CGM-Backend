using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Trash.Commands;

public record RestoreTrashItemCommand(TrashEntityType EntityType, Guid Id) : IRequest;

public class RestoreTrashItemCommandHandler : IRequestHandler<RestoreTrashItemCommand>
{
    private readonly IApplicationDbContext _db;

    public RestoreTrashItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(RestoreTrashItemCommand request, CancellationToken ct)
    {
        switch (request.EntityType)
        {
            case TrashEntityType.Album:
                await RestoreAsync(_db.Albums, request.Id, ct);
                break;
            case TrashEntityType.Product:
                await RestoreAsync(_db.Products, request.Id, ct);
                break;
            case TrashEntityType.MediaItem:
                await RestoreAsync(_db.MediaItems, request.Id, ct);
                break;
            case TrashEntityType.FAQ:
                await RestoreAsync(_db.FAQs, request.Id, ct);
                break;
            case TrashEntityType.Event:
                await RestoreAsync(_db.Events, request.Id, ct);
                break;
            case TrashEntityType.BlogPost:
                await RestoreAsync(_db.BlogPosts, request.Id, ct);
                break;
            case TrashEntityType.Booking:
                await RestoreAsync(_db.Bookings, request.Id, ct);
                break;
            case TrashEntityType.ContactMessage:
                await RestoreAsync(_db.ContactMessages, request.Id, ct);
                break;
            case TrashEntityType.Volunteer:
                await RestoreAsync(_db.Volunteers, request.Id, ct);
                break;
            case TrashEntityType.PrayerRequest:
                await RestoreAsync(_db.PrayerRequests, request.Id, ct);
                break;
            case TrashEntityType.TicketReservation:
                await RestoreAsync(_db.TicketReservations, request.Id, ct);
                break;
            case TrashEntityType.Subscriber:
                await RestoreAsync(_db.Subscribers, request.Id, ct);
                break;
            case TrashEntityType.Comment:
                await RestoreAsync(_db.Comments, request.Id, ct);
                break;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static async Task RestoreAsync<TEntity>(DbSet<TEntity> set, Guid id, CancellationToken ct)
        where TEntity : Domain.Entities.AuditableEntity
    {
        var entity = await set.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException("Trash item not found.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
    }
}
