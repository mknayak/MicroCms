using MicroCMS.Domain.Events;
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
            .IsRequired();

        builder.Property(m => m.DispatchMode)
            .IsRequired()
            .HasDefaultValue(OutboxDispatchMode.Exclusive);

        builder.Property(m => m.TenantId);
        builder.Property(m => m.OccurredOnUtc).IsRequired();
        builder.Property(m => m.ProcessedOnUtc);

        builder.Property(m => m.Error)
            .HasMaxLength(2000);

        builder.Property(m => m.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Exclusive dispatcher: unprocessed Exclusive rows ordered by time.
        builder.HasIndex(m => new { m.DispatchMode, m.ProcessedOnUtc, m.OccurredOnUtc });

        // Per-tenant dispatch: list unprocessed messages for a tenant
        builder.HasIndex(m => new { m.TenantId, m.DispatchMode, m.ProcessedOnUtc });

        builder.HasMany<OutboxDeliveryRecord>()
            .WithOne()
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
