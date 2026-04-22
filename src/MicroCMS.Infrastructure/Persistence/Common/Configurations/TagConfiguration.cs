using MicroCMS.Domain.Aggregates.Taxonomy;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="Tag"/> aggregate root.
/// Tags are flat (non-hierarchical) and site-scoped.
/// </summary>
internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TagId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(t => t.SiteId)
            .HasConversion(id => id.Value, value => new SiteId(value))
            .IsRequired();

        builder.Property(t => t.Name)
            .HasMaxLength(Tag.MaxNameLength)
            .IsRequired();

        builder.Property(t => t.Slug)
            .HasConversion(
                slug => slug.Value,
                value => Slug.Create(value))
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired();

        // Unique slug per site
        builder.HasIndex(t => new { t.SiteId, t.Slug }).IsUnique();

        builder.Ignore(t => t.DomainEvents);
    }
}
