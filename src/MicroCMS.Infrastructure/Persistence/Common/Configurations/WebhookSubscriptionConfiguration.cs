using MicroCMS.Domain.Aggregates.Webhooks;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
         .HasConversion(id => id.Value, value => new WebhookSubscriptionId(value))
                 .ValueGeneratedNever();

        builder.Property(w => w.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
     .IsRequired();

        builder.Property(w => w.SiteId)
            .HasConversion(
            id => id.HasValue ? (Guid?)id.Value.Value : null,
    value => value.HasValue ? new SiteId(value.Value) : (SiteId?)null);

        builder.Property(w => w.TargetUrl)
                .HasMaxLength(WebhookSubscription.MaxUrlLength)
                  .IsRequired();

        builder.Property(w => w.HashedSecret)
        .HasMaxLength(256)
            .IsRequired();

        builder.Property(w => w.IsActive).IsRequired();
        builder.Property(w => w.MaxRetries).IsRequired();
        builder.Property(w => w.CreatedAt).IsRequired();

        // Events list stored as pipe-separated string via private backing field
        var listConverter = new ValueConverter<List<string>, string>(
            list => string.Join('|', list),
            raw => raw.Length == 0
   ? new List<string>()
    : raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());

        builder.Property<List<string>>("_events")
  .HasField("_events")
  .UsePropertyAccessMode(PropertyAccessMode.Field)
  .HasConversion(listConverter)
       .HasColumnName("Events")
            .HasMaxLength(2000)
    .IsRequired()
   .HasDefaultValue(new List<string>());

        builder.Ignore(w => w.Events);

        // DeliveryLogs stored as owned collection
        builder.OwnsMany(w => w.DeliveryLogs, log =>
           {
               log.ToTable("WebhookDeliveryLogs");
               log.WithOwner().HasForeignKey("WebhookSubscriptionId");
               log.HasKey("WebhookSubscriptionId", nameof(WebhookDeliveryLog.DeliveredAt));
               log.Property(l => l.EventType).HasMaxLength(200).IsRequired();
               log.Property(l => l.StatusCode).IsRequired();
               log.Property(l => l.ErrorMessage).HasMaxLength(1000);
               log.Property(l => l.DeliveredAt).IsRequired();
               log.Ignore(l => l.IsSuccess);
           });

        builder.Ignore(w => w.DomainEvents);
    }
}
