using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
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
        builder.Property(c => c.ZonesJson).HasColumnType("TEXT").IsRequired();
        builder.Property(c => c.UsageCount).IsRequired();
        builder.Property(c => c.ItemCount).IsRequired();
     builder.Property(c => c.TemplateType).HasConversion<string>().HasMaxLength(30).IsRequired();
    builder.Property(c => c.TemplateContent).HasColumnType("TEXT");

      builder.Property(c => c.BackingContentTypeId)
     .HasConversion(
  id => id.HasValue ? id.Value.Value : (Guid?)null,
  v => v.HasValue ? new ContentTypeId(v.Value) : (ContentTypeId?)null);

    builder.Property(c => c.CreatedAt).IsRequired();
    builder.Property(c => c.UpdatedAt).IsRequired();

        // FieldDefinitions owned collection
    builder.OwnsMany(c => c.Fields, fieldBuilder =>
        {
  fieldBuilder.ToTable("ComponentFields");
            fieldBuilder.WithOwner().HasForeignKey("ComponentId");
         fieldBuilder.HasKey(f => f.Id);
            fieldBuilder.Property(f => f.Id).ValueGeneratedNever();

   fieldBuilder.Property(f => f.ContentTypeId)
 .HasConversion(id => id.Value, value => new ContentTypeId(value))
     .IsRequired();

 fieldBuilder.Property(f => f.Handle).HasMaxLength(FieldDefinition.MaxHandleLength).IsRequired();
            fieldBuilder.Property(f => f.Label).HasMaxLength(FieldDefinition.MaxLabelLength).IsRequired();
  fieldBuilder.Property(f => f.FieldType).HasConversion<string>().HasMaxLength(32).IsRequired();
   fieldBuilder.Property(f => f.IsRequired).IsRequired();
  fieldBuilder.Property(f => f.IsLocalized).IsRequired();
            fieldBuilder.Property(f => f.IsUnique).IsRequired();
            fieldBuilder.Property(f => f.SortOrder).IsRequired();
         fieldBuilder.Property(f => f.Description).HasMaxLength(FieldDefinition.MaxDescriptionLength);
            fieldBuilder.Property(f => f.ValidationJson);
        });

        builder.Ignore(c => c.DomainEvents);
    }
}

internal sealed class ComponentItemConfiguration : IEntityTypeConfiguration<ComponentItem>
{
    public void Configure(EntityTypeBuilder<ComponentItem> builder)
    {
        builder.ToTable("ComponentItems");

        builder.HasKey(ci => ci.Id);
        builder.Property(ci => ci.Id)
            .HasConversion(id => id.Value, value => new ComponentItemId(value))
 .ValueGeneratedNever();

        builder.Property(ci => ci.ComponentId)
    .HasConversion(id => id.Value, value => new ComponentId(value))
     .IsRequired();

        builder.Property(ci => ci.TenantId)
   .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

   builder.Property(ci => ci.SiteId)
        .HasConversion(id => id.Value, value => new SiteId(value))
        .IsRequired();

     builder.Property(ci => ci.Title).HasMaxLength(ComponentItem.MaxTitleLength).IsRequired();
        builder.Property(ci => ci.FieldsJson).HasColumnType("TEXT").IsRequired();
        builder.Property(ci => ci.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(ci => ci.UsedOnPages).IsRequired();
        builder.Property(ci => ci.CreatedAt).IsRequired();
     builder.Property(ci => ci.UpdatedAt).IsRequired();

     builder.HasIndex(ci => new { ci.ComponentId, ci.Status });
    }
}
