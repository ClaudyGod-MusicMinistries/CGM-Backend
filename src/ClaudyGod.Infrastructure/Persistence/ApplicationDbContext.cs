using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaudyGod.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUser;
    private readonly IDateTimeService? _clock;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUser = null,
        IDateTimeService? clock = null)
        : base(options)
    {
        _currentUser = currentUser;
        _clock = clock;
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
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PaystackPayment> PaystackPayments => Set<PaystackPayment>();
    public DbSet<FAQ> FAQs => Set<FAQ>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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
        modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PaystackPayment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FAQ>().HasQueryFilter(e => !e.IsDeleted && e.IsPublished);
        modelBuilder.Entity<Album>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Comment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UploadSession>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<BlogPostTag>().HasQueryFilter(e => !e.BlogPost.IsDeleted);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => !e.User.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock?.UtcNow ?? DateTime.UtcNow;
        var actor = _currentUser?.UserId ?? "system";
        foreach (var entry in ChangeTracker.Entries<Domain.Entities.AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = actor;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = actor;
                    break;
            }
        }

        var eventSources = ChangeTracker.Entries<Domain.Entities.BaseEntity>().ToList();
        foreach (var domainEvent in eventSources.SelectMany(x => x.Entity.DomainEvents))
        {
            OutboxMessages.Add(new OutboxMessage
            {
                Kind = "domain-event",
                Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!,
                Payload = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredAt = now,
                AvailableAt = now
            });
        }
        foreach (var source in eventSources)
            source.Entity.ClearDomainEvents();

        return await base.SaveChangesAsync(cancellationToken);
    }
}
