using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="Folder"/> aggregate (GAP-02).
/// Supports self-referencing parent/child hierarchy.
/// </summary>
internal sealed class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
 builder.ToTable("Folders");

     builder.HasKey(f => f.Id);
  builder.Property(f => f.Id)
  .HasConversion(id => id.Value, value => new FolderId(value))
     .ValueGeneratedNever();

     builder.Property(f => f.TenantId)
 .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

  builder.Property(f => f.SiteId)
     .HasConversion(id => id.Value, value => new SiteId(value))
   .IsRequired();

     builder.Property(f => f.Name)
        .HasMaxLength(Folder.MaxNameLength)
            .IsRequired();

        builder.Property(f => f.ParentFolderId)
            .HasConversion(
     id => id.HasValue ? id.Value.Value : (Guid?)null,
    value => value.HasValue ? new FolderId(value.Value) : (FolderId?)null);

        builder.Property(f => f.CreatedAt).IsRequired();
   builder.Property(f => f.UpdatedAt).IsRequired();

    builder.HasIndex(f => new { f.TenantId, f.SiteId, f.ParentFolderId });

 builder.Ignore(f => f.DomainEvents);
    }
}
