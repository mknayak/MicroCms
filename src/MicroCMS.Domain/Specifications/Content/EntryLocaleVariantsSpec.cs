using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>
/// Returns all locale variants of an entry — i.e. all entries sharing the same
/// SiteId, ContentTypeId, and Slug, regardless of locale.
/// Used to populate <c>EntryDto.LocaleVariants</c>.
/// </summary>
public sealed class EntryLocaleVariantsSpec : BaseSpecification<Entry>
{
    public EntryLocaleVariantsSpec(SiteId siteId, ContentTypeId contentTypeId, Slug slug)
     : base(e => e.SiteId == siteId
     && e.ContentTypeId == contentTypeId
      && e.Slug == slug) { }
}
