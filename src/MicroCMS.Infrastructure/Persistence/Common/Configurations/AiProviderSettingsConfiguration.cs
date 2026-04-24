using MicroCMS.Domain.Aggregates.Ai;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class AiProviderSettingsConfiguration : IEntityTypeConfiguration<AiProviderSettings>
{
    public void Configure(EntityTypeBuilder<AiProviderSettings> builder)
    {
        builder.ToTable("AiProviderSettings");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
          .HasConversion(id => id.Value, value => new AiProviderSettingsId(value))
            .ValueGeneratedNever();

        builder.Property(a => a.TenantId)
         .HasConversion(id => id.Value, value => new TenantId(value))
   .IsRequired();

        builder.Property(a => a.ActiveProvider)
        .HasMaxLength(100)
           .IsRequired();

        builder.Property(a => a.UpdatedAt).IsRequired();

        // AiBudget — owned entity, flattened columns
        builder.OwnsOne(a => a.Budget, budget =>
        {
            budget.Property(b => b.MonthlyCostCapUsd)
              .HasColumnName("Budget_MonthlyCostCapUsd")
           .HasColumnType("decimal(18,4)")
              .IsRequired();

            budget.Property(b => b.PerUserDailyTokenCap)
         .HasColumnName("Budget_PerUserDailyTokenCap")
                  .IsRequired();

            budget.Property(b => b.HardStop)
       .HasColumnName("Budget_HardStop")
     .IsRequired();

            budget.Property(b => b.CurrentMonthSpendUsd)
                .HasColumnName("Budget_CurrentMonthSpendUsd")
           .HasColumnType("decimal(18,4)")
             .IsRequired();
        });

        // AiSafetyConfig — owned entity, flattened columns
        builder.OwnsOne(a => a.Safety, safety =>
        {
            safety.Property(s => s.PiiRedactionEnabled).HasColumnName("Safety_PiiRedactionEnabled").IsRequired();
            safety.Property(s => s.PromptInjectionDetectionEnabled).HasColumnName("Safety_PromptInjectionDetectionEnabled").IsRequired();
            safety.Property(s => s.SafetyPostFilterEnabled).HasColumnName("Safety_SafetyPostFilterEnabled").IsRequired();
            safety.Property(s => s.GroundedOnlyModeDefault).HasColumnName("Safety_GroundedOnlyModeDefault").IsRequired();
            safety.Property(s => s.DataResidencyRegion).HasColumnName("Safety_DataResidencyRegion").HasMaxLength(10);
            safety.Property(s => s.AuditLogRetentionDays).HasColumnName("Safety_AuditLogRetentionDays").IsRequired();
        });

        // Model overrides — owned collection
        builder.OwnsMany(a => a.ModelOverrides, ov =>
        {
            ov.ToTable("AiModelTierOverrides");
            ov.WithOwner().HasForeignKey("AiProviderSettingsId");
            ov.HasKey("AiProviderSettingsId", nameof(AiModelTierOverride.FeatureKey));
            ov.Property(o => o.FeatureKey).HasMaxLength(100).IsRequired();
            ov.Property(o => o.Model).HasMaxLength(200).IsRequired();
        });

        builder.HasIndex(a => a.TenantId).IsUnique();
        builder.Ignore(a => a.DomainEvents);
    }
}
