using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class TicketReservationConfiguration : IEntityTypeConfiguration<TicketReservation>
{
    public void Configure(EntityTypeBuilder<TicketReservation> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.AttendeeFirstName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.AttendeeLastName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.AttendeeEmail).HasMaxLength(256).IsRequired();
        builder.Property(t => t.AttendeePhone).HasMaxLength(30).IsRequired();
        builder.Property(t => t.ConfirmationCode).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.CheckedInBy).HasMaxLength(200);

        builder.HasOne(t => t.Event)
               .WithMany()
               .HasForeignKey(t => t.EventId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => t.ConfirmationCode).IsUnique();
        builder.HasIndex(t => t.AttendeeEmail);
        builder.HasIndex(t => t.EventId);
        builder.HasIndex(t => new { t.EventId, t.Status });
    }
}
