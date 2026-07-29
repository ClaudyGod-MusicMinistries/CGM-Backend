using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Application.Features.Trash;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Trash.Commands;

public record EmptyTrashCommand : IRequest;

public class EmptyTrashCommandHandler : IRequestHandler<EmptyTrashCommand>
{
    private readonly IApplicationDbContext _db;

    public EmptyTrashCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(EmptyTrashCommand request, CancellationToken ct)
    {
        // Purges anything already past the 30-day window, then hard-deletes
        // everything else currently soft-deleted — this command's whole job.
        await TrashPurge.PurgeExpiredAsync(_db, ct);

        _db.Albums.RemoveRange(await _db.Albums.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.Products.RemoveRange(await _db.Products.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.MediaItems.RemoveRange(await _db.MediaItems.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.FAQs.RemoveRange(await _db.FAQs.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.Events.RemoveRange(await _db.Events.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.BlogPosts.RemoveRange(await _db.BlogPosts.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.Bookings.RemoveRange(await _db.Bookings.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.ContactMessages.RemoveRange(await _db.ContactMessages.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.Volunteers.RemoveRange(await _db.Volunteers.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.PrayerRequests.RemoveRange(await _db.PrayerRequests.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.TicketReservations.RemoveRange(await _db.TicketReservations.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.Subscribers.RemoveRange(await _db.Subscribers.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));
        _db.Comments.RemoveRange(await _db.Comments.IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(ct));

        await _db.SaveChangesAsync(ct);
    }
}
