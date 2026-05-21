using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderReference).HasMaxLength(50).IsRequired();
        builder.HasIndex(o => o.OrderReference).IsUnique();
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(o => o.ShippingInfo, s =>
        {
            s.Property(x => x.FirstName).HasMaxLength(100).HasColumnName("ShipFirstName");
            s.Property(x => x.LastName).HasMaxLength(100).HasColumnName("ShipLastName");
            s.Property(x => x.Email).HasMaxLength(256).HasColumnName("ShipEmail");
            s.Property(x => x.Phone).HasMaxLength(30).HasColumnName("ShipPhone");
            s.Property(x => x.Address).HasMaxLength(300).HasColumnName("ShipAddress");
            s.Property(x => x.City).HasMaxLength(100).HasColumnName("ShipCity");
            s.Property(x => x.State).HasMaxLength(100).HasColumnName("ShipState");
            s.Property(x => x.Country).HasMaxLength(100).HasColumnName("ShipCountry");
            s.Property(x => x.NearestLocation).HasMaxLength(200).HasColumnName("ShipNearestLocation");
        });

        // Map private backing fields for collections
        builder.Navigation(o => o.Items).HasField("_items");
        builder.Navigation(o => o.Payments).HasField("_payments");

        builder.HasMany<OrderItem>().WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany<Payment>().WithOne().HasForeignKey("OrderId");
    }
}
