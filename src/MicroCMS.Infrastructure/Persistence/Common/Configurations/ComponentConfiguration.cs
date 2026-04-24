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
builder.Property(c => c.Zone).HasMaxLength(Component.MaxZoneLength).IsRequired();
        builder.Property(c => c.UsageCount).IsRequired();
     builder.Property(c => c.CreatedAt).IsRequired();
    builder.Property(c => c.UpdatedAt).IsRequired();

        // FieldDefinitions owned collection (same schema shape as ContentType fields)
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
