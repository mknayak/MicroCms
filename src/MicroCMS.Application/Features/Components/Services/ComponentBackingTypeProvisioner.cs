using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Components.Commands;
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
    public async Task<ContentType> ProvisionAsync(
        Component component,
        IReadOnlyList<ComponentFieldInput>? initialFields = null,
        CancellationToken ct = default)
    {
        if (component.BackingContentTypeId is not null)
        {
            // Already provisioned — just load and return it
            return await contentTypeRepo.GetByIdAsync(component.BackingContentTypeId.Value, ct)
                ?? throw new InvalidOperationException($"Backing ContentType '{component.BackingContentTypeId}' not found.");
        }

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

        // Seed fields from the create request onto the backing ContentType
        if (initialFields is { Count: > 0 })
        {
            for (int i = 0; i < initialFields.Count; i++)
            {
                var f = initialFields[i];
                if (!Enum.TryParse<FieldType>(f.FieldType, true, out var ft))
                    ft = FieldType.ShortText;
                contentType.AddField(f.Handle, f.Label, ft, f.IsRequired, f.IsLocalized, f.IsUnique, f.Description, null, f.IsIndexed, f.IsList);
            }
        }

        await contentTypeRepo.AddAsync(contentType, ct);

        component.SetBackingContentType(contentType.Id);
        // Do NOT call componentRepo.Update here — the component is already tracked
        // by EF (state: Added or Modified). Calling Update() would forcibly set the
        // state to Modified, which causes a DbUpdateConcurrencyException when EF
        // tries to UPDATE a row that has not been inserted yet.

        await unitOfWork.SaveChangesAsync(ct);
        return contentType;
    }
}
