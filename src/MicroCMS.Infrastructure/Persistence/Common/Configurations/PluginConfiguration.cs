using MicroCMS.Domain.Aggregates.Plugins;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class PluginConfiguration : IEntityTypeConfiguration<Plugin>
{
 public void Configure(EntityTypeBuilder<Plugin> builder)
{
     builder.ToTable("Plugins");

  builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
    .HasConversion(id => id.Value, value => new PluginId(value))
  .ValueGeneratedNever();

  builder.Property(p => p.TenantId)
     .HasConversion(id => id.Value, value => new TenantId(value))
    .IsRequired();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Version).HasMaxLength(50).IsRequired();
  builder.Property(p => p.Author).HasMaxLength(200).IsRequired();
      builder.Property(p => p.Signature).HasMaxLength(500);
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.InstalledAt).IsRequired();

     // Capabilities — private backing field stored as pipe-separated string
    var listConverter = new ValueConverter<List<string>, string>(
      list => string.Join('|', list),
    raw => raw.Length == 0
        ? new List<string>()
    : raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());

builder.Property<List<string>>("_capabilities")
    .HasField("_capabilities")
   .UsePropertyAccessMode(PropertyAccessMode.Field)
    .HasConversion(listConverter)
   .HasColumnName("Capabilities")
    .HasMaxLength(4000)
    .IsRequired()
    .HasDefaultValue(new List<string>());

     builder.Ignore(p => p.Capabilities);
        builder.Ignore(p => p.DomainEvents);
    }
}
