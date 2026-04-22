using MicroCMS.Application.Features.Entries.Queries.Preview;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Infrastructure.Content;

/// <summary>
/// Provides the per-site HMAC secret for signing preview tokens.
/// Derives a deterministic secret from the SiteId; replace with a
/// vault-backed implementation for production deployments.
/// </summary>
internal sealed class SiteIdPreviewSecretProvider(
    IRepository<Site, SiteId> siteRepository)
    : IPreviewSecretProvider
{
    public async Task<string> GetSiteSecretAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var site = await siteRepository.GetByIdAsync(new SiteId(siteId), cancellationToken);

        // Fall back to a deterministic secret derived from the SiteId when the
        // site record is not available (e.g. in tests or before full provisioning).
        return site is not null
    ? $"preview-secret-{site.Id.Value}"
     : $"preview-secret-{siteId}";
    }
}
