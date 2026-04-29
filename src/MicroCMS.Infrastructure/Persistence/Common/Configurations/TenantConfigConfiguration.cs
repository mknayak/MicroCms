using MicroCMS.Domain.Aggregates.Settings;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class TenantConfigConfiguration : IEntityTypeConfiguration<TenantConfig>
{
    public void Configure(EntityTypeBuilder<TenantConfig> builder)
    {
        builder.ToTable("TenantConfigs");

        // PK is TenantId — 1-to-1 with Tenant, no separate surrogate key needed.
        builder.HasKey(tc => tc.Id);
        builder.Property(tc => tc.Id)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .ValueGeneratedNever();

        builder.Property(tc => tc.UpdatedAt).IsRequired();

        // ── Owned config entries ──────────────────────────────────────────
        builder.OwnsMany(tc => tc.Entries, entry =>
        {
            entry.ToTable("TenantConfigEntries");

            entry.WithOwner().HasForeignKey("TenantConfigId");

            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id)
                .HasConversion(id => id.Value, value => new ConfigEntryId(value))
                .ValueGeneratedNever();

            entry.Property(e => e.Key)
                .HasMaxLength(ConfigEntry.MaxKeyLength)
                .IsRequired();

            entry.Property(e => e.Value)
                .HasMaxLength(ConfigEntry.MaxValueLength)
                .IsRequired();

            entry.Property(e => e.Category)
                .HasMaxLength(ConfigEntry.MaxCategoryLength)
                .IsRequired()
                .HasDefaultValue("general");

            entry.Property(e => e.IsSecret).IsRequired();
            entry.Property(e => e.UpdatedAt).IsRequired();

            entry.HasIndex("TenantConfigId", nameof(ConfigEntry.Key)).IsUnique();
        });

        builder.Navigation(tc => tc.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_entries");
    }
}
