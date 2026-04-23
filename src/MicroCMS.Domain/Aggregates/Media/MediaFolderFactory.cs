using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Media;

/// <summary>
/// Static factory that wraps <see cref="MediaFolder"/>'s internal constructor so
/// Application-layer handlers can create and mutate folders without exposing
/// those concerns directly on the aggregate.
/// </summary>
public static class MediaFolderFactory
{
    public static MediaFolder Create(
        TenantId tenantId,
        SiteId siteId,
        string name,
        Guid? parentFolderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        return new MediaFolder(Guid.NewGuid(), tenantId, siteId, name.Trim(), parentFolderId);
    }

    public static void Rename(MediaFolder folder, string newName) =>
        folder.Rename(newName);

    public static void Move(MediaFolder folder, Guid? newParentFolderId) =>
        folder.Move(newParentFolderId);
}
