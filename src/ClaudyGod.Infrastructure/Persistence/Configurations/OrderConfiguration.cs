using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderId).HasMaxLength(100).IsRequired();
        builder.Property(o => o.FullName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Email).HasMaxLength(255).IsRequired();
        builder.Property(o => o.Phone).HasMaxLength(20).IsRequired();
        builder.Property(o => o.ShippingAddress).HasMaxLength(500).IsRequired();
        builder.Property(o => o.City).HasMaxLength(100).IsRequired();
        builder.Property(o => o.State).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Country).HasMaxLength(100).IsRequired();
        builder.Property(o => o.PostalCode).HasMaxLength(20);
        builder.Property(o => o.ItemsJson).IsRequired();
        builder.Property(o => o.ShippingMethod).HasMaxLength(50).IsRequired();
        builder.Property(o => o.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(o => o.Subtotal).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.ShippingCost).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.Total).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(10).IsRequired();
        builder.Property(o => o.PaystackReference).HasMaxLength(100);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.AdminNotes).HasMaxLength(500);

        builder.HasQueryFilter(o => !o.IsDeleted);
        builder.HasIndex(o => o.OrderId).IsUnique();
        builder.HasIndex(o => o.Email);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAt);
    }
}
