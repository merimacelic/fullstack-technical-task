using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Users;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(v => v.Value, v => new RefreshTokenId(v))
            .ValueGeneratedNever();

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.FamilyId)
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).IsRequired();
        builder.Property(t => t.RevokedAtUtc);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);

        builder.Ignore(t => t.DomainEvents);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);
        // Covers the replay-detection family-revoke query.
        builder.HasIndex(t => new { t.UserId, t.FamilyId });
    }
}
