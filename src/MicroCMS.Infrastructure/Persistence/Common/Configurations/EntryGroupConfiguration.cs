using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="EntryGroup"/> aggregate and its
/// owned <see cref="EntryGroupMember"/> join records.
/// </summary>
internal sealed class EntryGroupConfiguration : IEntityTypeConfiguration<EntryGroup>
{
    public void Configure(EntityTypeBuilder<EntryGroup> builder)
    {
        builder.ToTable("EntryGroups");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasConversion(id => id.Value, value => new EntryGroupId(value))
            .ValueGeneratedNever();

        builder.Property(g => g.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(g => g.SiteId)
            .HasConversion(id => id.Value, value => new SiteId(value))
            .IsRequired();

        builder.Property(g => g.ContentTypeId)
            .HasConversion(id => id.Value, value => new ContentTypeId(value))
            .IsRequired();

        builder.Property(g => g.Handle)
            .HasMaxLength(EntryGroup.MaxHandleLength)
            .IsRequired();

        builder.Property(g => g.Title)
            .HasMaxLength(EntryGroup.MaxTitleLength)
            .IsRequired();

        builder.Property(g => g.Description)
            .HasMaxLength(EntryGroup.MaxDescriptionLength);

        builder.Property(g => g.ImageAssetId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new MediaAssetId(value.Value) : (MediaAssetId?)null);

        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt).IsRequired();

        // Unique constraint: handle per site + content type
        builder.HasIndex(g => new { g.SiteId, g.ContentTypeId, g.Handle }).IsUnique();

        builder.Ignore(g => g.DomainEvents);

        // ── Members (owned collection) ─────────────────────────────────────
        builder.OwnsMany(g => g.Members, member =>
        {
            member.ToTable("EntryGroupMembers");

            member.WithOwner()
                .HasForeignKey("GroupId");

            member.HasKey("GroupId", "EntryId");

            member.Property(m => m.GroupId)
                .HasConversion(id => id.Value, value => new EntryGroupId(value))
                .IsRequired();

            member.Property(m => m.EntryId)
                .HasConversion(id => id.Value, value => new EntryId(value))
                .IsRequired();
        });
    }
}
