using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
{
    public void Configure(EntityTypeBuilder<ApiClient> builder)
    {
        builder.ToTable("ApiClients");

     builder.HasKey(a => a.Id);
  builder.Property(a => a.Id)
    .HasConversion(id => id.Value, value => new ApiClientId(value))
      .ValueGeneratedNever();

    builder.Property(a => a.TenantId)
     .HasConversion(id => id.Value, value => new TenantId(value))
        .IsRequired();

   builder.Property(a => a.SiteId)
       .HasConversion(id => id.Value, value => new SiteId(value))
           .IsRequired();

   builder.Property(a => a.Name).HasMaxLength(ApiClient.MaxNameLength).IsRequired();

        builder.Property(a => a.KeyType)
     .HasConversion<string>()
   .HasMaxLength(32)
  .IsRequired();

    builder.Property(a => a.HashedSecret).HasMaxLength(256).IsRequired();
  builder.Property(a => a.IsActive).IsRequired();
        builder.Property(a => a.ExpiresAt);
       builder.Property(a => a.CreatedAt).IsRequired();

        // Scopes — private backing field stored as pipe-separated string
   var listConverter = new ValueConverter<List<string>, string>(
 list => string.Join('|', list),
   raw => raw.Length == 0
    ? new List<string>()
    : raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());

  builder.Property<List<string>>("_scopes")
      .HasField("_scopes")
   .UsePropertyAccessMode(PropertyAccessMode.Field)
.HasConversion(listConverter)
  .HasColumnName("Scopes")
        .HasMaxLength(2000)
   .IsRequired()
    .HasDefaultValue(new List<string>());

      builder.Ignore(a => a.Scopes);
      builder.Ignore(a => a.DomainEvents);
    }
}
