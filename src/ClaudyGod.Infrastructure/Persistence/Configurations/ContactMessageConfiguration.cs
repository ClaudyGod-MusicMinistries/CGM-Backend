using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Message).HasMaxLength(5000).IsRequired();

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.IsRead);
        builder.HasIndex(c => c.CreatedAt);
    }
}
