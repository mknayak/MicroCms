using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="ContentType"/> aggregate root.
/// FieldDefinitions are owned as a collection in a separate table.
/// The global multi-tenant query filter is applied by <see cref="ApplicationDbContext"/>.
/// </summary>
internal sealed class ContentTypeConfiguration : IEntityTypeConfiguration<ContentType>
{
    public void Configure(EntityTypeBuilder<ContentType> builder)
    {
        builder.ToTable("ContentTypes");

        builder.HasKey(ct => ct.Id);
        builder.Property(ct => ct.Id)
            .HasConversion(id => id.Value, value => new ContentTypeId(value))
            .ValueGeneratedNever();

        builder.Property(ct => ct.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(ct => ct.SiteId)
            .HasConversion(id => id.Value, value => new SiteId(value))
            .IsRequired();

        builder.Property(ct => ct.Handle)
            .HasMaxLength(ContentType.MaxHandleLength)
            .IsRequired();

        builder.Property(ct => ct.DisplayName)
            .HasMaxLength(ContentType.MaxDisplayNameLength)
            .IsRequired();

        builder.Property(ct => ct.Description)
            .HasMaxLength(ContentType.MaxDescriptionLength);

        builder.Property(ct => ct.LocalizationMode)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(LocalizationMode.PerLocale);

        builder.Property(ct => ct.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(ct => ct.Kind)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(ContentTypeKind.Content);

        builder.Property(ct => ct.LayoutId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                v => v.HasValue ? new LayoutId(v.Value) : (LayoutId?)null);

        builder.Property(ct => ct.CreatedAt).IsRequired();
        builder.Property(ct => ct.UpdatedAt).IsRequired();

        // Unique constraint: handle must be unique per site
        builder.HasIndex(ct => new { ct.SiteId, ct.Handle }).IsUnique();

        // ── FieldDefinitions (owned collection) ───────────────────────────
        builder.OwnsMany(ct => ct.Fields, field =>
        {
            field.ToTable("ContentTypeFields");

            field.WithOwner()
                .HasForeignKey("ContentTypeId");

            field.HasKey(f => f.Id);
            field.Property(f => f.Id)
                .ValueGeneratedNever();

            field.Property(f => f.ContentTypeId)
                .HasConversion(id => id.Value, value => new ContentTypeId(value))
                .IsRequired();

            field.Property(f => f.Handle)
                .HasMaxLength(FieldDefinition.MaxHandleLength)
                .IsRequired();

            field.Property(f => f.Label)
                .HasMaxLength(FieldDefinition.MaxLabelLength)
                .IsRequired();

            field.Property(f => f.FieldType)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            field.Property(f => f.IsRequired).IsRequired();
            field.Property(f => f.IsLocalized).IsRequired();
            field.Property(f => f.IsUnique).IsRequired();
            field.Property(f => f.IsIndexed).IsRequired();
            field.Property(f => f.SortOrder).IsRequired();

            field.Property(f => f.Description)
                .HasMaxLength(FieldDefinition.MaxDescriptionLength);

            field.Property(f => f.ValidationJson);

            // Handle must be unique within a content type
            field.HasIndex("ContentTypeId", "Handle").IsUnique();
        });

        // ── Ignore domain events (not persisted on aggregate) ─────────────
        builder.Ignore(ct => ct.DomainEvents);
    }
}
