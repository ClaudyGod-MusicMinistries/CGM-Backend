using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class VolunteerConfiguration : IEntityTypeConfiguration<Volunteer>
{
    public void Configure(EntityTypeBuilder<Volunteer> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(v => v.LastName).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Email).HasMaxLength(256).IsRequired();
        builder.Property(v => v.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(v => v.Reason).HasMaxLength(2000).IsRequired();

        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.HasIndex(v => v.Email);
        builder.HasIndex(v => v.Role);
        builder.HasIndex(v => v.IsApproved);
        builder.HasIndex(v => new { v.Role, v.IsApproved });
    }
}
