using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="MediaAsset"/> aggregate root.
/// AssetMetadata is stored as an owned entity (flattened columns).
/// Tags (_tags backing field) are stored as a pipe-separated string via value converter.
/// </summary>
internal sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new MediaAssetId(value))
            .ValueGeneratedNever();

        builder.Property(a => a.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(a => a.SiteId)
            .HasConversion(id => id.Value, value => new SiteId(value))
            .IsRequired();

        builder.Property(a => a.StorageKey)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(a => a.FolderId);
        builder.Property(a => a.UploadedBy).IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.AltText).HasMaxLength(500);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        // ── AssetMetadata (owned entity — flattened columns) ─────────────
        builder.OwnsOne(a => a.Metadata, meta =>
        {
            meta.Property(m => m.FileName)
                .HasColumnName("Meta_FileName")
                .HasMaxLength(AssetMetadata.MaxFileNameLength)
                .IsRequired();

            meta.Property(m => m.MimeType)
                .HasColumnName("Meta_MimeType")
                .HasMaxLength(AssetMetadata.MaxMimeTypeLength)
                .IsRequired();

            meta.Property(m => m.SizeBytes)
                .HasColumnName("Meta_SizeBytes")
                .IsRequired();

            meta.Property(m => m.WidthPx)
                .HasColumnName("Meta_WidthPx");

            meta.Property(m => m.HeightPx)
                .HasColumnName("Meta_HeightPx");

            meta.Property(m => m.Duration)
                .HasColumnName("Meta_Duration");

            meta.Property(m => m.ExifData)
                .HasColumnName("Meta_ExifJson")
                .HasConversion(
                    dict => System.Text.Json.JsonSerializer.Serialize(
                        dict, (System.Text.Json.JsonSerializerOptions?)null),
                    json => (IReadOnlyDictionary<string, string>)(
                        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                            json, (System.Text.Json.JsonSerializerOptions?)null)
                        ?? new Dictionary<string, string>()))
                .HasColumnType("nvarchar(max)");
        });

        // ── Tags — map private backing field _tags via value converter ────
        // EF Core 8 can access private fields with UsePropertyAccessMode(Field).
        var tagsConverter = new ValueConverter<List<string>, string>(
            tags => string.Join('|', tags),
            raw => raw.Length == 0
                ? new List<string>()
                : raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.Property<List<string>>("_tags")
            .HasField("_tags")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(tagsConverter)
            .HasColumnName("Tags")
            .HasMaxLength(2000)
            .IsRequired()
            .HasDefaultValue(new List<string>());

        // Suppress mapping of the public IReadOnlyList<string> Tags property
        builder.Ignore(a => a.Tags);

        builder.Ignore(a => a.DomainEvents);
    }
}
