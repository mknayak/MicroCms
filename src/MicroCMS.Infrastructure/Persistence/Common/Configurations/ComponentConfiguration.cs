using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
    public void Configure(EntityTypeBuilder<Component> builder)
    {
        builder.ToTable("Components");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
   .HasConversion(id => id.Value, value => new ComponentId(value))
            .ValueGeneratedNever();

      builder.Property(c => c.TenantId)
  .HasConversion(id => id.Value, value => new TenantId(value))
       .IsRequired();

        builder.Property(c => c.SiteId)
       .HasConversion(id => id.Value, value => new SiteId(value))
            .IsRequired();

        builder.Property(c => c.Name).HasMaxLength(Component.MaxNameLength).IsRequired();
        builder.Property(c => c.Key).HasMaxLength(Component.MaxKeyLength).IsRequired();
      builder.Property(c => c.Description).HasMaxLength(Component.MaxDescriptionLength);
 builder.Property(c => c.Category).HasMaxLength(Component.MaxCategoryLength).IsRequired();
        builder.Property("ZonesJson").HasColumnType("TEXT").IsRequired().HasDefaultValue("[]");
        builder.Property(c => c.UsageCount).IsRequired();
        builder.Property(c => c.ItemCount).IsRequired();
     builder.Property(c => c.TemplateType).HasConversion<string>().HasMaxLength(30).IsRequired();
    builder.Property(c => c.TemplateContent).HasColumnType("TEXT");
        builder.Property(c => c.ThumbnailDataUri).HasColumnType("TEXT");

      builder.Property(c => c.BackingContentTypeId)
     .HasConversion(
  id => id.HasValue ? id.Value.Value : (Guid?)null,
  v => v.HasValue ? new ContentTypeId(v.Value) : (ContentTypeId?)null);

            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.UpdatedAt).IsRequired();

            builder.Ignore(c => c.DomainEvents);
        }
    }
