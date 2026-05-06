using MicroCMS.Infrastructure.Storage.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class MediaBlobRecordConfiguration : IEntityTypeConfiguration<MediaBlobRecord>
{
    public void Configure(EntityTypeBuilder<MediaBlobRecord> builder)
    {
        builder.ToTable("MediaBlobs");

        builder.HasKey(b => b.StorageKey);

        builder.Property(b => b.StorageKey)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(b => b.MimeType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(b => b.Data)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();
    }
}
