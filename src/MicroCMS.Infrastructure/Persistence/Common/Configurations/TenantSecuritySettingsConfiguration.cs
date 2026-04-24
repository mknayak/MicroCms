using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class TenantSecuritySettingsConfiguration : IEntityTypeConfiguration<TenantSecuritySettings>
{
 public void Configure(EntityTypeBuilder<TenantSecuritySettings> builder)
    {
 builder.ToTable("TenantSecuritySettings");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
        .HasConversion(id => id.Value, value => new TenantSecuritySettingsId(value))
   .ValueGeneratedNever();

builder.Property(t => t.TenantId)
     .HasConversion(id => id.Value, value => new TenantId(value))
    .IsRequired();

        builder.Property(t => t.RequireMfaForAdmins).IsRequired();
   builder.Property(t => t.SsoEnabled).IsRequired();
      builder.Property(t => t.OidcIssuer).HasMaxLength(500);
   builder.Property(t => t.UpdatedAt).IsRequired();

// TimeSpan stored as total seconds (long) — portable across all DB providers
        builder.Property(t => t.SessionIdleTimeout)
      .HasConversion(
   ts => (long)ts.TotalSeconds,
  seconds => TimeSpan.FromSeconds(seconds))
            .IsRequired();

   builder.Property(t => t.AbsoluteSessionTimeout)
    .HasConversion(
         ts => (long)ts.TotalSeconds,
 seconds => TimeSpan.FromSeconds(seconds))
     .IsRequired();

 // IP allowlist stored as pipe-separated string via private backing field
 var listConverter = new ValueConverter<List<string>, string>(
   list => string.Join('|', list),
     raw => raw.Length == 0
     ? new List<string>()
        : raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.Property<List<string>>("_ipAllowlist")
   .HasField("_ipAllowlist")
      .UsePropertyAccessMode(PropertyAccessMode.Field)
    .HasConversion(listConverter)
       .HasColumnName("IpAllowlist")
      .HasMaxLength(4000)
    .IsRequired()
     .HasDefaultValue(new List<string>());

    builder.Ignore(t => t.IpAllowlist);

        builder.HasIndex(t => t.TenantId).IsUnique();
        builder.Ignore(t => t.DomainEvents);
    }
}
