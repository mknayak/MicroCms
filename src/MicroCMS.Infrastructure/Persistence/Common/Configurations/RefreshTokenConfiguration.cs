using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new RefreshTokenId(value));

        builder.Property(t => t.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(t => t.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .HasMaxLength(64) // SHA-256 hex = 64 chars
            .IsRequired();

        builder.Property(t => t.FamilyId)
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.Property(t => t.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(64);

        // Fast lookup by token hash (primary query path)
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // Fast revocation of an entire rotation family
        builder.HasIndex(t => t.FamilyId);

        // Cleanup index for expired / revoked tokens
        builder.HasIndex(t => new { t.UserId, t.IsRevoked });
    }
}
