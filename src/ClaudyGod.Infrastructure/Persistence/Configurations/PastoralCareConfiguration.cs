using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class PastoralCareConfiguration : IEntityTypeConfiguration<PastoralCareRequest>
{
    public void Configure(EntityTypeBuilder<PastoralCareRequest> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).HasMaxLength(50).IsRequired();
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Phone).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(300).IsRequired();
        builder.Property(p => p.EventType).HasMaxLength(100).IsRequired();
        builder.Property(p => p.ChurchRole).HasMaxLength(100).IsRequired();
        builder.Property(p => p.CustomRole).HasMaxLength(100);
        builder.Property(p => p.Comments).HasMaxLength(2000);
        builder.Property(p => p.SourceChannel).HasMaxLength(100);
        builder.Property(p => p.Status).HasMaxLength(20).IsRequired();
        builder.HasIndex(p => p.Status);
    }
}
