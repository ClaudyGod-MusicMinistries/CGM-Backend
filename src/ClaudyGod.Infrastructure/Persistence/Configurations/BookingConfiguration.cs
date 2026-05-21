using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(b => b.LastName).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Email).HasMaxLength(256).IsRequired();
        builder.Property(b => b.Phone).HasMaxLength(30).IsRequired();
        builder.Property(b => b.Organization).HasMaxLength(200);
        builder.Property(b => b.OrgType).HasMaxLength(100);
        builder.Property(b => b.EventType).HasMaxLength(100);
        builder.Property(b => b.EventDetails).HasMaxLength(2000);
        builder.Property(b => b.CountryCode).HasConversion<string>().HasMaxLength(10);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(b => b.Address, a =>
        {
            a.Property(x => x.Address1).HasMaxLength(200).HasColumnName("AddressLine1");
            a.Property(x => x.Address2).HasMaxLength(200).HasColumnName("AddressLine2");
            a.Property(x => x.City).HasMaxLength(100).HasColumnName("City");
            a.Property(x => x.State).HasMaxLength(100).HasColumnName("State");
            a.Property(x => x.ZipCode).HasMaxLength(20).HasColumnName("ZipCode");
            a.Property(x => x.Country).HasMaxLength(100).HasColumnName("Country");
        });
    }
}
