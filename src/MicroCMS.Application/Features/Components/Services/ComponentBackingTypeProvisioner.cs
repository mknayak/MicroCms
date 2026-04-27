using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Components.Services;

/// <summary>
/// When a new Component is created, this service auto-creates a matching
/// <see cref="ContentType"/> with <see cref="ContentTypeKind.Component"/>
/// and links it to the Component via <see cref="Component.BackingContentTypeId"/>.
///
/// Field definitions stay on the ContentType (single source of truth for data schema).
/// The Component entity stores only visual/rendering concerns.
/// </summary>
public sealed class ComponentBackingTypeProvisioner(
    IRepository<ContentType, ContentTypeId> contentTypeRepo,
    IRepository<Component, ComponentId> componentRepo,
    IUnitOfWork unitOfWork)
{
    public async Task ProvisionAsync(Component component, CancellationToken ct = default)
    {
        if (component.BackingContentTypeId is not null)
  return; // already provisioned

    // Derive a unique handle from the component key
        var handle = $"__comp_{component.Key.Replace("-", "_")}";

        var contentType = ContentType.Create(
      component.TenantId,
  component.SiteId,
          handle,
    $"{component.Name} (Component Data)",
            $"Auto-created backing type for component '{component.Key}'.",
     LocalizationMode.Shared,
            ContentTypeKind.Component);

    await contentTypeRepo.AddAsync(contentType, ct);

      component.SetBackingContentType(contentType.Id);
      componentRepo.Update(component);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
