using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="User"/> aggregate root.
/// Roles are owned as a collection in a separate table.
/// EmailAddress and PersonName are stored as owned entities (flattened columns).
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => new UserId(value))
            .ValueGeneratedNever();

        builder.Property(u => u.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        // ── EmailAddress (value object → single column) ───────────────────
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => EmailAddress.Create(value))
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();

        // Unique email per tenant
        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();

        // ── PersonName (value object → single column) ─────────────────────
        builder.Property(u => u.DisplayName)
            .HasConversion(
                name => name.Value,
                value => PersonName.Create(value))
            .HasMaxLength(PersonName.MaxLength)
            .IsRequired();

        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        // ── Password credential (nullable for OIDC-only accounts) ─────────
        builder.Property(u => u.PasswordHash)
            .HasMaxLength(72) // bcrypt output is always ≤ 72 chars
            .IsRequired(false);

        builder.Property(u => u.PasswordChangedAt)
            .IsRequired(false);

        // ── Brute-force lockout ───────────────────────────────────────────
        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.LockoutEnd)
            .IsRequired(false);

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);

        // ── Roles (owned collection in separate table) ─────────────────────
        builder.OwnsMany(u => u.Roles, role =>
        {
            role.ToTable("UserRoles");

            role.WithOwner()
                .HasForeignKey("UserId");

            role.HasKey(r => r.Id);
            role.Property(r => r.Id)
                .HasConversion(id => id.Value, value => new RoleId(value))
                .ValueGeneratedNever();

            role.Property(r => r.TenantId)
                .HasConversion(id => id.Value, value => new TenantId(value))
                .IsRequired();

            role.Property(r => r.WorkflowRole)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            role.Property(r => r.Name)
                .HasMaxLength(Role.MaxNameLength)
                .IsRequired();

            role.Property(r => r.SiteId)
                .HasConversion(
                    id => id.HasValue ? (Guid?)id.Value.Value : null,
                    value => value.HasValue ? new SiteId(value.Value) : (SiteId?)null);

            role.Property(r => r.CreatedAt).IsRequired();

            // Computed property — ignore
            role.Ignore(r => r.IsTenantWide);
        });

        builder.Ignore(u => u.DomainEvents);
    }
}
