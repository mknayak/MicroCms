using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class SiteTemplateConfiguration : IEntityTypeConfiguration<SiteTemplate>
{
    public void Configure(EntityTypeBuilder<SiteTemplate> builder)
    {
        builder.ToTable("SiteTemplates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
          .HasConversion(id => id.Value, v => new SiteTemplateId(v))
 .ValueGeneratedNever();

        builder.Property(t => t.TenantId)
            .HasConversion(id => id.Value, v => new TenantId(v))
            .IsRequired();

        builder.Property(t => t.SiteId)
       .HasConversion(id => id.Value, v => new SiteId(v))
            .IsRequired();

        builder.Property(t => t.LayoutId)
       .HasConversion(id => id.Value, v => new LayoutId(v))
   .IsRequired();

        builder.Property(t => t.Name)
     .HasMaxLength(200)
         .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

      builder.Property(t => t.PlacementsJson)
     .HasColumnType("TEXT")
    .IsRequired();

        builder.HasIndex(t => new { t.TenantId, t.SiteId });

        builder.Ignore(t => t.DomainEvents);
    }
}
