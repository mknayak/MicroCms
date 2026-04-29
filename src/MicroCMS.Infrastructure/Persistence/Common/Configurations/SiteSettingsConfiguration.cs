using MicroCMS.Domain.Aggregates.Settings;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
   builder.ToTable("SiteSettings");

  builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
    .HasConversion(id => id.Value, value => new SiteId(value))
         .ValueGeneratedNever();

   builder.Property(s => s.TenantId)
 .HasConversion(id => id.Value, value => new TenantId(value))
    .IsRequired();

        builder.Property(s => s.PreviewUrlTemplate).HasMaxLength(500);
    builder.Property(s => s.VersioningEnabled).IsRequired();
        builder.Property(s => s.WorkflowEnabled).IsRequired();
  builder.Property(s => s.SchedulingEnabled).IsRequired();
  builder.Property(s => s.PreviewEnabled).IsRequired();
   builder.Property(s => s.AiEnabled).IsRequired();
   builder.Property(s => s.UpdatedAt).IsRequired();

  var listConverter = new ValueConverter<List<string>, string>(
   list => string.Join('|', list),
     raw => raw.Length == 0
         ? new List<string>()
          : raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());

  // CORS origins — private backing field
    builder.Property<List<string>>("_corsOrigins")
      .HasField("_corsOrigins")
    .UsePropertyAccessMode(PropertyAccessMode.Field)
     .HasConversion(listConverter)
   .HasColumnName("CorsOrigins")
    .HasMaxLength(4000)
       .IsRequired()
      .HasDefaultValue(new List<string>());

      builder.Ignore(s => s.CorsOrigins);

     // Locales — private backing field
    builder.Property<List<string>>("_locales")
        .HasField("_locales")
  .UsePropertyAccessMode(PropertyAccessMode.Field)
     .HasConversion(listConverter)
    .HasColumnName("Locales")
      .HasMaxLength(1024)
    .IsRequired()
  .HasDefaultValue(new List<string>());

     builder.Ignore(s => s.Locales);

        // ── Owned site-level config entries (GAP-AI-1) ───────────────────
        builder.OwnsMany(s => s.ConfigEntries, entry =>
        {
            entry.ToTable("SiteConfigEntries");

            entry.WithOwner().HasForeignKey("SiteSettingsId");

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

            entry.HasIndex("SiteSettingsId", nameof(ConfigEntry.Key)).IsUnique();
        });

        builder.Navigation(s => s.ConfigEntries)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_configEntries");

        builder.Ignore(s => s.DomainEvents);
    }
}
