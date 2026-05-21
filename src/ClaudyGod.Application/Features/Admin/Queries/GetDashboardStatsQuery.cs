using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Features.Admin.Queries;

public record DashboardStatsDto(
    int TotalSubscribers,
    int ActiveSubscribers,
    int TotalBookings,
    int PendingBookings,
    int TotalVolunteers,
    int PendingVolunteers,
    int TotalEvents,
    int UpcomingEvents,
    int TotalTickets,
    int TotalPrayerRequests,
    int PendingPrayerRequests,
    int TotalContactMessages,
    int UnreadMessages,
    int TotalMediaItems,
    int TotalBlogPosts,
    int PublishedBlogPosts);

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IApplicationDbContext _db;

    public GetDashboardStatsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        var subscribers = await _db.Subscribers.CountAsync(ct);
        var activeSubscribers = await _db.Subscribers.CountAsync(s => s.IsActive, ct);

        var bookings = await _db.Bookings.CountAsync(ct);
        var pendingBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending, ct);

        var volunteers = await _db.Volunteers.CountAsync(ct);
        var pendingVolunteers = await _db.Volunteers.CountAsync(v => !v.IsApproved, ct);

        var events = await _db.Events.CountAsync(ct);
        var upcomingEvents = await _db.Events.CountAsync(e => e.Status == EventStatus.Upcoming, ct);

        var tickets = await _db.TicketReservations.CountAsync(ct);

        var prayers = await _db.PrayerRequests.CountAsync(ct);
        var pendingPrayers = await _db.PrayerRequests
            .CountAsync(p => p.Status == PrayerRequestStatus.Received, ct);

        var contacts = await _db.ContactMessages.CountAsync(ct);
        var unreadContacts = await _db.ContactMessages.CountAsync(c => !c.IsRead, ct);

        var media = await _db.MediaItems.CountAsync(ct);

        var blogs = await _db.BlogPosts.CountAsync(ct);
        var publishedBlogs = await _db.BlogPosts
            .CountAsync(b => b.Status == BlogPostStatus.Published, ct);

        return new DashboardStatsDto(
            subscribers, activeSubscribers,
            bookings, pendingBookings,
            volunteers, pendingVolunteers,
            events, upcomingEvents,
            tickets,
            prayers, pendingPrayers,
            contacts, unreadContacts,
            media,
            blogs, publishedBlogs);
    }
}
