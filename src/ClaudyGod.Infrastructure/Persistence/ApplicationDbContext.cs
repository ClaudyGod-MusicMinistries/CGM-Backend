using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IMediator? _mediator;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMediator? mediator = null)
        : base(options)
    {
        _mediator = mediator;
    }

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
    public DbSet<Reel> Reels => Set<Reel>();
    public DbSet<Album> Albums => Set<Album>();

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
        modelBuilder.Entity<Reel>().HasQueryFilter(e => !e.IsDeleted);
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

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after the transaction commits so handlers see
        // the persisted state. Fire-and-forget failures are logged, not thrown.
        if (_mediator is not null)
        {
            var events = ChangeTracker.Entries<Domain.Entities.BaseEntity>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            foreach (var entity in ChangeTracker.Entries<Domain.Entities.BaseEntity>())
                entity.Entity.ClearDomainEvents();

            foreach (var domainEvent in events)
                await _mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}
