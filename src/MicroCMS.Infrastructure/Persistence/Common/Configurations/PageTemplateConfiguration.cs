using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class PageTemplateConfiguration : IEntityTypeConfiguration<PageTemplate>
{
    public void Configure(EntityTypeBuilder<PageTemplate> builder)
    {
        builder.ToTable("PageTemplates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
   .HasConversion(id => id.Value, value => new PageTemplateId(value))
            .ValueGeneratedNever();

    builder.Property(t => t.TenantId)
         .HasConversion(id => id.Value, value => new TenantId(value))
    .IsRequired();

        builder.Property(t => t.PageId)
   .HasConversion(id => id.Value, value => new PageId(value))
       .IsRequired();

    // Primary placement storage as a JSON tree
        builder.Property(t => t.PlacementsJson)
            .HasColumnType("TEXT")
     .IsRequired()
            .HasDefaultValue("[]");

    builder.Property(t => t.UpdatedAt).IsRequired();

        // Legacy flat placements table kept for backwards compat / EF nav
        builder.OwnsMany(t => t.Placements, placement =>
        {
            placement.ToTable("PageTemplatePlacements");
     placement.WithOwner().HasForeignKey("PageTemplateId");
            placement.HasKey(p => p.Id);
     placement.Property(p => p.Id).ValueGeneratedNever();

    placement.Property(p => p.ComponentId)
            .HasConversion(id => id.Value, value => new ComponentId(value))
                .IsRequired();

            placement.Property(p => p.Zone)
        .HasMaxLength(200)
         .IsRequired();

placement.Property(p => p.SortOrder).IsRequired();

            placement.Property(p => p.BoundItemId)
     .HasConversion(
   id => id.HasValue ? id.Value.Value : (Guid?)null,
  v => v.HasValue ? new ComponentItemId(v.Value) : (ComponentItemId?)null);

            placement.Property(p => p.IsLayoutDefault).IsRequired().HasDefaultValue(false);
        });

        builder.Ignore(t => t.DomainEvents);
    }
}
