using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class AiDailyUsageConfiguration : IEntityTypeConfiguration<MicroCMS.Infrastructure.Ai.AiDailyUsage>
{
    public void Configure(EntityTypeBuilder<MicroCMS.Infrastructure.Ai.AiDailyUsage> builder)
    {
        builder.ToTable("AiDailyUsage");

        builder.HasKey(e => new { e.TenantId, e.UserId, e.Date });

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.TotalTokens).IsRequired().HasDefaultValue(0L);
        builder.Property(e => e.TotalCostUsd).IsRequired().HasColumnType("decimal(18,6)").HasDefaultValue(0m);
    }
}
