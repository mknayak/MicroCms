using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="Tenant"/> aggregate root.
/// TenantSettings and TenantQuota are stored as owned entities (separate columns).
/// Sites are owned as a collection (own table with FK to Tenant).
/// </summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // ── TenantSlug (value object → column) ───────────────────────────
        builder.Property(t => t.Slug)
            .HasConversion(
                slug => slug.Value,
                value => TenantSlug.Create(value))
            .HasMaxLength(TenantSlug.MaxLength)
            .IsRequired();

        builder.HasIndex(t => t.Slug).IsUnique();

        // ── TenantSettings (owned entity — flattened to columns) ─────────
        builder.OwnsOne(t => t.Settings, settings =>
        {
            settings.Property(s => s.DisplayName)
                .HasColumnName("Settings_DisplayName")
                .HasMaxLength(TenantSettings.MaxDisplayNameLength)
                .IsRequired();

            settings.Property(s => s.DefaultLocale)
                .HasColumnName("Settings_DefaultLocale")
                .HasConversion(
                    locale => locale.Value,
                    value => Locale.Create(value))
                .HasMaxLength(Locale.MaxLength)
                .IsRequired();

            // Enabled locales stored as comma-separated string (simple and portable)
            settings.Property(s => s.EnabledLocales)
                .HasColumnName("Settings_EnabledLocales")
                .HasConversion(
                    locales => string.Join(',', locales.Select(l => l.Value)),
                    raw => (IReadOnlyList<Locale>)raw
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(Locale.Create)
                        .ToList())
                .HasMaxLength(1024)
                .IsRequired();

            settings.Property(s => s.TimeZoneId)
                .HasColumnName("Settings_TimeZoneId")
                .HasMaxLength(TenantSettings.MaxTimeZoneIdLength)
                .IsRequired();

            settings.Property(s => s.AiEnabled)
                .HasColumnName("Settings_AiEnabled")
                .IsRequired();

            settings.Property(s => s.LogoUrl)
                .HasColumnName("Settings_LogoUrl")
                .HasMaxLength(2048);
        });

        // ── TenantQuota (owned entity — flattened to columns) ─────────────
        builder.OwnsOne(t => t.Quota, quota =>
        {
            quota.Property(q => q.MaxStorageBytes).HasColumnName("Quota_MaxStorageBytes").IsRequired();
            quota.Property(q => q.MaxApiCallsPerMinute).HasColumnName("Quota_MaxApiCallsPerMinute").IsRequired();
            quota.Property(q => q.MaxUsers).HasColumnName("Quota_MaxUsers").IsRequired();
            quota.Property(q => q.MaxSites).HasColumnName("Quota_MaxSites").IsRequired();
            quota.Property(q => q.MaxContentTypes).HasColumnName("Quota_MaxContentTypes").IsRequired();
            quota.Property(q => q.MaxAiTokensPerMonth).HasColumnName("Quota_MaxAiTokensPerMonth").IsRequired();
        });

        // ── Sites (owned collection — table-splitting) ───────────────────
        builder.OwnsMany(t => t.Sites, site =>
        {
            site.ToTable("Sites");

            site.WithOwner()
                .HasForeignKey("TenantId");

            site.HasKey(s => s.Id);
            site.Property(s => s.Id)
                .HasConversion(id => id.Value, value => new SiteId(value))
                .ValueGeneratedNever();

            site.Property(s => s.TenantId)
                .HasConversion(id => id.Value, value => new TenantId(value))
                .IsRequired();

            site.Property(s => s.Name)
                .HasMaxLength(100)
                .IsRequired();

            site.Property(s => s.Handle)
                .HasConversion(
                    slug => slug.Value,
                    value => Slug.Create(value))
                .HasMaxLength(Slug.MaxLength)
                .IsRequired();

            site.Property(s => s.DefaultLocale)
                .HasConversion(
                    locale => locale.Value,
                    value => Locale.Create(value))
                .HasMaxLength(Locale.MaxLength)
                .IsRequired();

            site.Property(s => s.CustomDomain)
                .HasConversion(
                    domain => domain == null ? null : domain.Value,
                    raw => raw == null ? null : CustomDomain.Create(raw))
                .HasMaxLength(CustomDomain.MaxLength);

            site.Property(s => s.IsActive).IsRequired();
            site.Property(s => s.CreatedAt).IsRequired();

            site.HasIndex("TenantId", "Handle").IsUnique();

            // ── SiteEnvironments (owned collection inside owned Site) ─────
            site.OwnsMany(s => s.Environments, env =>
            {
                env.ToTable("SiteEnvironments");
                env.WithOwner().HasForeignKey("SiteId");
                env.HasKey("SiteId", nameof(SiteEnvironment.Type));

                env.Property(e => e.Type)
                    .HasConversion<string>()
                    .HasMaxLength(32)
                    .IsRequired();

                env.Property(e => e.Url)
                    .HasMaxLength(500)
                    .IsRequired();

                env.Property(e => e.SslStatus)
                    .HasConversion<string>()
                    .HasMaxLength(32)
                    .IsRequired();

                env.Property(e => e.IsLive).IsRequired();
            });
        });

        // Tenant inherits AggregateRoot which exposes DomainEvents — not persisted
        builder.Ignore(t => t.DomainEvents);
    }
}
