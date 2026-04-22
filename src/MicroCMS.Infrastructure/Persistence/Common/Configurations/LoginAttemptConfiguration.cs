using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new LoginAttemptId(value));

        builder.Property(a => a.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(a => a.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.IsSuccessful)
            .IsRequired();

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 max = 45 chars

        builder.Property(a => a.UserAgent)
            .HasMaxLength(512);

        builder.Property(a => a.AttemptedAt)
            .IsRequired();

        // Audit / brute-force analysis indexes
        builder.HasIndex(a => new { a.TenantId, a.Email, a.AttemptedAt });
        builder.HasIndex(a => a.AttemptedAt); // for TTL cleanup jobs
    }
}
