using System.Text.Json;
using MicroCMS.Domain.Aggregates;
using MicroCMS.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MicroCMS.Infrastructure.Persistence.Common.Interceptors;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> that converts domain events
/// raised on aggregate roots into <see cref="OutboxMessage"/> rows within the same transaction.
/// This guarantees at-least-once delivery without a separate distributed transaction.
/// </summary>
internal sealed class DomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ConvertDomainEventsToOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ConvertDomainEventsToOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static void ConvertDomainEventsToOutboxMessages(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var aggregateRoots = context.ChangeTracker
    .Entries<IAggregateRoot>()
          .Where(entry => entry.Entity.DomainEvents.Count > 0)
      .Select(entry => entry.Entity)
      .ToList();

        var outboxMessages = aggregateRoots
            .SelectMany(root =>
  {
            var tenantId = ExtractTenantId(root);
                return root.DomainEvents.Select(domainEvent => CreateOutboxMessage(domainEvent, tenantId));
            })
            .ToList();

        foreach (var aggregate in aggregateRoots)
        {
         aggregate.ClearDomainEvents();
        }

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }

    private static OutboxMessage CreateOutboxMessage(IDomainEvent domainEvent, Guid? tenantId)
    {
        var type = domainEvent.GetType().AssemblyQualifiedName
            ?? domainEvent.GetType().FullName
            ?? domainEvent.GetType().Name;

        var content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);

        return new OutboxMessage(
            id: Guid.NewGuid(),
            type: type,
            content: content,
            tenantId: tenantId,
            occurredOnUtc: domainEvent.OccurredOn);
    }

    /// <summary>
    /// Reflects tenant ID from well-known aggregate properties (TenantId).
    /// Returns null for system-level aggregates (e.g. Tenant itself during creation).
    /// </summary>
    private static Guid? ExtractTenantId(IAggregateRoot root)
    {
    var tenantIdProp = root.GetType().GetProperty("TenantId");
        if (tenantIdProp is null)
        {
   return null;
        }

      var value = tenantIdProp.GetValue(root);
        if (value is null)
        {
    return null;
     }

      // TenantId is a readonly record struct with a .Value (Guid) property
        var valueProp = value.GetType().GetProperty("Value");
  return valueProp?.GetValue(value) as Guid?;
    }
}
