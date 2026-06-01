using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class SubscriberConfiguration : IEntityTypeConfiguration<Subscriber>
{
    public void Configure(EntityTypeBuilder<Subscriber> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Email).HasMaxLength(256).IsRequired();
        builder.Property(s => s.UnsubscribeToken).HasMaxLength(64).IsRequired();

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => s.Email).IsUnique();
        builder.HasIndex(s => s.UnsubscribeToken).IsUnique();
        builder.HasIndex(s => s.IsActive);
    }
}
