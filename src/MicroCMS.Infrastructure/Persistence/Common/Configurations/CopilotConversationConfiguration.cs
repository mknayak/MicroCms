using MicroCMS.Domain.Aggregates.Ai;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class CopilotConversationConfiguration : IEntityTypeConfiguration<CopilotConversation>
{
    public void Configure(EntityTypeBuilder<CopilotConversation> builder)
    {
        builder.ToTable("CopilotConversations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CopilotConversationId(value))
      .ValueGeneratedNever();

     builder.Property(c => c.TenantId)
   .HasConversion(id => id.Value, value => new TenantId(value))
    .IsRequired();

        builder.Property(c => c.UserId).IsRequired();
  builder.Property(c => c.GroundedOnlyMode).IsRequired();
   builder.Property(c => c.TotalPromptTokens).IsRequired();
        builder.Property(c => c.TotalCompletionTokens).IsRequired();
        builder.Property(c => c.TotalCostUsd).HasColumnType("decimal(18,6)").IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
   builder.Property(c => c.LastMessageAt);

        builder.OwnsMany(c => c.Messages, msg =>
        {
msg.ToTable("CopilotMessages");
          msg.WithOwner().HasForeignKey("ConversationId");
 msg.HasKey(m => m.Id);
      msg.Property(m => m.Id).ValueGeneratedNever();
   msg.Property(m => m.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            msg.Property(m => m.Content).IsRequired();
  msg.Property(m => m.CreatedAt).IsRequired();

     msg.OwnsMany(m => m.Citations, cit =>
            {
cit.ToTable("CopilotMessageCitations");
                cit.WithOwner().HasForeignKey("MessageId");
              cit.HasKey("MessageId", nameof(CopilotCitation.EntryId));
    cit.Property(c => c.EntryId).IsRequired();
            cit.Property(c => c.Slug).HasMaxLength(500).IsRequired();
    cit.Property(c => c.Title).HasMaxLength(500).IsRequired();
     cit.Property(c => c.SimilarityScore).IsRequired();
   });
        });
    }
}
