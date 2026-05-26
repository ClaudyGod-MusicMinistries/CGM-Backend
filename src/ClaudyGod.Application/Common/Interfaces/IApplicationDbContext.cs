using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Subscriber> Subscribers { get; }
    DbSet<ContactMessage> ContactMessages { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Volunteer> Volunteers { get; }
    DbSet<ZellePayment> ZellePayments { get; }
    DbSet<NigerianBankTransfer> NigerianBankTransfers { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PrayerRequest> PrayerRequests { get; }
    DbSet<MediaItem> MediaItems { get; }
    DbSet<BlogPost> BlogPosts { get; }
    DbSet<BlogCategory> BlogCategories { get; }
    DbSet<BlogTag> BlogTags { get; }
    DbSet<BlogPostTag> BlogPostTags { get; }
    DbSet<Event> Events { get; }
    DbSet<TicketReservation> TicketReservations { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Reel> Reels { get; }
    DbSet<Album> Albums { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
