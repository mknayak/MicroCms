using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for <see cref="MediaFolder"/>.
/// Supports self-referencing parent/child hierarchy via <see cref="MediaFolder.ParentFolderId"/>.
/// </summary>
internal sealed class MediaFolderConfiguration : IEntityTypeConfiguration<MediaFolder>
{
    public void Configure(EntityTypeBuilder<MediaFolder> builder)
    {
        builder.ToTable("MediaFolders");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .ValueGeneratedNever();

        builder.Property(f => f.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(f => f.SiteId)
            .HasConversion(id => id.Value, value => new SiteId(value))
            .IsRequired();

        builder.Property(f => f.Name)
            .HasMaxLength(MediaFolder.MaxNameLength)
            .IsRequired();

        builder.Property(f => f.ParentFolderId);
        builder.Property(f => f.CreatedAt).IsRequired();

        // Index for listing a folder's children quickly
        builder.HasIndex(f => new { f.TenantId, f.SiteId, f.ParentFolderId });
    }
}
