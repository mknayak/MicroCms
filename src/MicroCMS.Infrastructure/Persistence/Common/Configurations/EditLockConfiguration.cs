using MicroCMS.Domain.Aggregates.Locks;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCMS.Infrastructure.Persistence.Common.Configurations;

internal sealed class EditLockConfiguration : IEntityTypeConfiguration<EditLock>
{
    public void Configure(EntityTypeBuilder<EditLock> builder)
    {
        builder.ToTable("EditLocks");

        builder.HasKey(l => l.Id);
      builder.Property(l => l.Id)
   .HasConversion(id => id.Value, v => new EditLockId(v))
        .ValueGeneratedNever();

        builder.Property(l => l.EntityId)
      .HasMaxLength(200)
 .IsRequired();

        builder.Property(l => l.EntityType)
            .HasMaxLength(50)
     .IsRequired();

        builder.Property(l => l.LockedByUserId).IsRequired();

      builder.Property(l => l.LockedByDisplayName)
   .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.LockedAt).IsRequired();
   builder.Property(l => l.ExpiresAt).IsRequired();

        // Only one lock per entity at a time
    builder.HasIndex(l => l.EntityId).IsUnique();

        builder.Ignore(l => l.DomainEvents);
    }
}
