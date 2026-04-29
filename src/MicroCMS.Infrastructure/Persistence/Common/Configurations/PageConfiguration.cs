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

        // ── SEO metadata owned entity ──────────────────────────────────────
        builder.OwnsOne(p => p.Seo, seo =>
        {
            seo.Property(s => s.MetaTitle)
      .HasMaxLength(SeoMetadata.MaxMetaTitleLength)
        .HasColumnName("SeoMetaTitle");
     seo.Property(s => s.MetaDescription)
       .HasMaxLength(SeoMetadata.MaxMetaDescriptionLength)
         .HasColumnName("SeoMetaDescription");
       seo.Property(s => s.CanonicalUrl)
     .HasMaxLength(500)
 .HasColumnName("SeoCanonicalUrl");
 seo.Property(s => s.OgImage)
        .HasMaxLength(500)
         .HasColumnName("SeoOgImage");
        });

        builder.HasIndex(p => new { p.SiteId, p.Slug }).IsUnique();
        builder.HasIndex(p => new { p.SiteId, p.ParentId });

     builder.Ignore(p => p.DomainEvents);

        ConfigureNullableIds(builder);
    }

    private static void ConfigureNullableIds(EntityTypeBuilder<Page> builder)
    {
        ConfigureNullablePageId(builder);
        ConfigureNullableEntryId(builder);
        ConfigureNullableContentTypeId(builder);
        ConfigureNullableSiteTemplateId(builder);
    }

    private static void ConfigureNullablePageId(EntityTypeBuilder<Page> builder) =>
        builder.Property(p => p.ParentId)
            .HasConversion(
                id => id.HasValue ? (Guid?)id.Value.Value : null,
                v => v.HasValue ? new PageId(v.Value) : (PageId?)null);

    private static void ConfigureNullableEntryId(EntityTypeBuilder<Page> builder) =>
        builder.Property(p => p.LinkedEntryId)
            .HasConversion(
                id => id.HasValue ? (Guid?)id.Value.Value : null,
                v => v.HasValue ? new EntryId(v.Value) : (EntryId?)null);

    private static void ConfigureNullableContentTypeId(EntityTypeBuilder<Page> builder) =>
        builder.Property(p => p.CollectionContentTypeId)
            .HasConversion(
                id => id.HasValue ? (Guid?)id.Value.Value : null,
                v => v.HasValue ? new ContentTypeId(v.Value) : (ContentTypeId?)null);

    private static void ConfigureNullableSiteTemplateId(EntityTypeBuilder<Page> builder) =>
        builder.Property(p => p.SiteTemplateId)
            .HasConversion(
                id => id.HasValue ? (Guid?)id.Value.Value : null,
                v => v.HasValue ? new SiteTemplateId(v.Value) : (SiteTemplateId?)null);
}
