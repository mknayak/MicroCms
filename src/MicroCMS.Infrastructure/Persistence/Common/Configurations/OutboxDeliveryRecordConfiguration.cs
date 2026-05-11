using MicroCMS.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

/// <summary>
/// EF Core mapping for <see cref="OutboxDeliveryRecord"/>.
/// One row per (message, instance) pair; composite PK prevents duplicate processing.
/// </summary>
internal sealed class OutboxDeliveryRecordConfiguration : IEntityTypeConfiguration<OutboxDeliveryRecord>
{
    public void Configure(EntityTypeBuilder<OutboxDeliveryRecord> builder)
    {
        builder.ToTable("OutboxDeliveryRecords");

        builder.HasKey(r => new { r.MessageId, r.InstanceId });

        builder.Property(r => r.InstanceId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.ProcessedOnUtc).IsRequired();

        // Poller query: "undelivered Broadcast messages for this instance"
        // JOIN OutboxMessages ON Id = MessageId WHERE InstanceId = @me is covered by the PK.
        // This index covers the reverse look-up: all instances that processed a message.
        builder.HasIndex(r => r.MessageId);
    }
}
