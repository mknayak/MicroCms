using MicroCMS.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for <see cref="OutboxMessage"/>.
/// Indexes are tuned for the dispatcher query: unprocessed messages ordered by occurrence time.
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Type)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(m => m.TenantId);
        builder.Property(m => m.OccurredOnUtc).IsRequired();
        builder.Property(m => m.ProcessedOnUtc);

        builder.Property(m => m.Error)
            .HasMaxLength(2000);

        builder.Property(m => m.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Primary dispatcher index: fetch oldest unprocessed messages first.
        // Note: HasFilter with a SQL fragment is provider-specific (SQL Server syntax shown).
        // PostgreSQL and SQLite support partial indexes but with different syntax;
        // the filtered index can be added manually via a migration edit if needed.
        builder.HasIndex(m => new { m.ProcessedOnUtc, m.OccurredOnUtc });

        // Per-tenant dispatch: list unprocessed messages for a tenant
        builder.HasIndex(m => new { m.TenantId, m.ProcessedOnUtc });
    }
}
