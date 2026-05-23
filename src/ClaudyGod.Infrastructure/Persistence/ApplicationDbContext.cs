using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<ZellePayment> ZellePayments => Set<ZellePayment>();
    public DbSet<NigerianBankTransfer> NigerianBankTransfers => Set<NigerianBankTransfer>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PrayerRequest> PrayerRequests => Set<PrayerRequest>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogTag> BlogTags => Set<BlogTag>();
    public DbSet<BlogPostTag> BlogPostTags => Set<BlogPostTag>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<TicketReservation> TicketReservations => Set<TicketReservation>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<LeadershipMember> LeadershipMembers => Set<LeadershipMember>();
    public DbSet<Reel> Reels => Set<Reel>();
    public DbSet<GivingOption> GivingOptions => Set<GivingOption>();
    public DbSet<GivingIntent> GivingIntents => Set<GivingIntent>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<WorkforceApplication> WorkforceApplications => Set<WorkforceApplication>();
    public DbSet<PastoralCareRequest> PastoralCareRequests => Set<PastoralCareRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global soft-delete filter for auditable entities
        modelBuilder.Entity<Subscriber>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ContactMessage>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Booking>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Volunteer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PrayerRequest>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MediaItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<BlogPost>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Event>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TicketReservation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Testimonial>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LeadershipMember>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Reel>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<GivingOption>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<GivingIntent>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ContentItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkforceApplication>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PastoralCareRequest>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Entities.AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
