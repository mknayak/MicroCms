using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="Entry"/> aggregate root.
/// EntryVersions are owned in a separate table; versions are append-only.
/// </summary>
internal sealed class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.ToTable("Entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EntryId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.SiteId)
            .HasConversion(id => id.Value, value => new SiteId(value))
            .IsRequired();

        builder.Property(e => e.ContentTypeId)
            .HasConversion(id => id.Value, value => new ContentTypeId(value))
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasConversion(
                slug => slug.Value,
                value => Slug.Create(value))
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.Property(e => e.Locale)
            .HasConversion(
                locale => locale.Value,
                value => Locale.Create(value))
            .HasMaxLength(Locale.MaxLength)
            .IsRequired();

        builder.Property(e => e.AuthorId).IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.CurrentVersionNumber).IsRequired();

        // FieldsJson can be large — use TEXT / nvarchar(max)
        builder.Property(e => e.FieldsJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.PublishedAt);
        builder.Property(e => e.ScheduledPublishAt);
        builder.Property(e => e.ScheduledUnpublishAt);

        // ── GAP-02: FolderId ───────────────────────────────────────────────
        builder.Property(e => e.FolderId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new FolderId(value.Value) : (FolderId?)null);

        // ── GAP-08: SeoMetadata owned entity ──────────────────────────────
        builder.OwnsOne(e => e.Seo, seo =>
        {
            seo.Property(s => s.MetaTitle).HasMaxLength(SeoMetadata.MaxMetaTitleLength).HasColumnName("SeoMetaTitle");
            seo.Property(s => s.MetaDescription).HasMaxLength(SeoMetadata.MaxMetaDescriptionLength).HasColumnName("SeoMetaDescription");
            seo.Property(s => s.CanonicalUrl).HasMaxLength(500).HasColumnName("SeoCanonicalUrl");
            seo.Property(s => s.OgImage).HasMaxLength(500).HasColumnName("SeoOgImage");
        });

        // Unique: slug per site per locale (content invariant in same locale must have unique slug)
        builder.HasIndex(e => new { e.SiteId, e.Locale, e.Slug }).IsUnique();

        // ── EntryVersions (owned collection in separate table) ─────────────
        builder.OwnsMany(e => e.Versions, version =>
        {
            version.ToTable("EntryVersions");

            version.WithOwner()
                .HasForeignKey("EntryId");

            version.HasKey(v => v.Id);
            version.Property(v => v.Id).ValueGeneratedNever();

            version.Property(v => v.EntryId)
                .HasConversion(id => id.Value, value => new EntryId(value))
                .IsRequired();

            version.Property(v => v.VersionNumber).IsRequired();
            version.Property(v => v.AuthorId).IsRequired();

            version.Property(v => v.FieldsJson).HasColumnType("nvarchar(max)")
                .IsRequired();
        });
    }
}
