using ClaudyGod.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaudyGod.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Token).HasMaxLength(200).IsRequired();
        builder.Property(r => r.CreatedByIp).HasMaxLength(50);
        builder.Property(r => r.RevokedByIp).HasMaxLength(50);

        builder.Ignore(r => r.IsActive);

        builder.HasOne(r => r.User)
               .WithMany(u => u.RefreshTokens)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.Token).IsUnique();
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => new { r.UserId, r.IsRevoked, r.ExpiresAt });
    }
}
