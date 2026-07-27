using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(5000);
        builder.Property(e => e.Venue).HasMaxLength(300);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.TicketPrice).HasPrecision(18, 2);
        builder.Property(e => e.FlyerImagePath).HasMaxLength(1000);
        builder.Property(e => e.Version).IsRowVersion();
        builder.Ignore(e => e.AvailableSeats);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Events_TotalCapacity_NonNegative", "\"TotalCapacity\" >= 0");
            t.HasCheckConstraint("CK_Events_ReservedCount_WithinCapacity",
                "\"ReservedCount\" >= 0 AND \"ReservedCount\" <= \"TotalCapacity\"");
        });

        builder.OwnsOne(e => e.Location, l =>
        {
            l.Property(x => x.Address1).HasMaxLength(200).HasColumnName("LocationAddress1");
            l.Property(x => x.Address2).HasMaxLength(200).HasColumnName("LocationAddress2");
            l.Property(x => x.City).HasMaxLength(100).HasColumnName("LocationCity");
            l.Property(x => x.State).HasMaxLength(100).HasColumnName("LocationState");
            l.Property(x => x.ZipCode).HasMaxLength(20).HasColumnName("LocationZipCode");
            l.Property(x => x.Country).HasMaxLength(100).HasColumnName("LocationCountry");
        });

        builder.HasMany(e => e.Reservations)
            .WithOne(r => r.Event)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
