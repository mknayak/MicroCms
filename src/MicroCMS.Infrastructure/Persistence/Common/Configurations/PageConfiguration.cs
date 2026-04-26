using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("Pages");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
         .HasConversion(id => id.Value, value => new PageId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.TenantId)
        .HasConversion(id => id.Value, value => new TenantId(value))
    .IsRequired();

        builder.Property(p => p.SiteId)
 .HasConversion(id => id.Value, value => new SiteId(value))
            .IsRequired();

        builder.Property(p => p.Title)
            .HasMaxLength(Page.MaxTitleLength)
    .IsRequired();

        // Slug value object → single string column
        builder.Property(p => p.Slug)
  .HasConversion(
     slug => slug.Value,
           value => Slug.Create(value))
      .HasMaxLength(Slug.MaxLength)
     .IsRequired();

        builder.Property(p => p.PageType)
               .HasConversion<string>()
        .HasMaxLength(32)
          .IsRequired();

        builder.Property(p => p.RoutePattern)
            .HasMaxLength(Page.MaxRoutePatternLength);

        builder.Property(p => p.Depth).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

     // Optional layout override — null means use the site default layout
     builder.Property(p => p.LayoutId)
            .HasConversion(
       id => id.HasValue ? (Guid?)id.Value.Value : null,
         value => value.HasValue ? new LayoutId(value.Value) : (LayoutId?)null);

        builder.HasIndex(p => new { p.SiteId, p.Slug }).IsUnique();
        builder.HasIndex(p => new { p.SiteId, p.ParentId });

        builder.Ignore(p => p.DomainEvents);

          ConfigureNullableIds(builder);
    }

    private static void ConfigureNullableIds(EntityTypeBuilder<Page> builder)
    {
        builder.Property(p => p.ParentId)
    .HasConversion(id => id.HasValue ? (Guid?)id.Value.Value : null,
          v => v.HasValue ? new PageId(v.Value) : (PageId?)null);

     builder.Property(p => p.LinkedEntryId)
  .HasConversion(id => id.HasValue ? (Guid?)id.Value.Value : null,
       v => v.HasValue ? new EntryId(v.Value) : (EntryId?)null);

     builder.Property(p => p.CollectionContentTypeId)
         .HasConversion(id => id.HasValue ? (Guid?)id.Value.Value : null,
          v => v.HasValue ? new ContentTypeId(v.Value) : (ContentTypeId?)null);
    }
}
