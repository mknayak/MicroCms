using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class LayoutConfiguration : IEntityTypeConfiguration<Layout>
{
    public void Configure(EntityTypeBuilder<Layout> builder)
    {
        builder.ToTable("Layouts");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, v => new LayoutId(v))
            .ValueGeneratedNever();

        builder.Property(l => l.TenantId)
            .HasConversion(id => id.Value, v => new TenantId(v))
            .IsRequired();

        builder.Property(l => l.SiteId)
            .HasConversion(id => id.Value, v => new SiteId(v))
            .IsRequired();

        builder.Property(l => l.Name)
            .HasMaxLength(Layout.MaxNameLength)
            .IsRequired();

        builder.Property(l => l.Key)
            .HasMaxLength(Layout.MaxKeyLength)
            .IsRequired();

        builder.Property(l => l.TemplateType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.ShellTemplate)
            .HasColumnType("TEXT");

        builder.Property(l => l.IsDefault).IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();

        // Unique key per site — prevents two layouts sharing the same handle
        builder.HasIndex(l => new { l.SiteId, l.Key }).IsUnique();
    }
}
