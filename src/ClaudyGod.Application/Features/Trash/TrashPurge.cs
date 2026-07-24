using ClaudyGod.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Trash;

/// <summary>
/// "Lazy cleanup" for Trash — there is no background job/scheduler. Every entry
/// point into Trash (list, empty) calls this first, so anything past the 30-day
/// retention window is hard-deleted right before the caller sees fresh state.
/// </summary>
internal static class TrashPurge
{
    private const int RetentionDays = 30;

    public static async Task PurgeExpiredAsync(IApplicationDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

        db.Albums.RemoveRange(await db.Albums.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.Products.RemoveRange(await db.Products.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.MediaItems.RemoveRange(await db.MediaItems.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.FAQs.RemoveRange(await db.FAQs.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.Events.RemoveRange(await db.Events.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.BlogPosts.RemoveRange(await db.BlogPosts.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.Bookings.RemoveRange(await db.Bookings.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.ContactMessages.RemoveRange(await db.ContactMessages.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.Volunteers.RemoveRange(await db.Volunteers.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.PrayerRequests.RemoveRange(await db.PrayerRequests.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.TicketReservations.RemoveRange(await db.TicketReservations.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.Subscribers.RemoveRange(await db.Subscribers.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));
        db.Comments.RemoveRange(await db.Comments.IgnoreQueryFilters()
            .Where(x => x.IsDeleted && x.DeletedAt < cutoff).ToListAsync(ct));

        await db.SaveChangesAsync(ct);
    }

    public static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength].TrimEnd() + "…";
}
