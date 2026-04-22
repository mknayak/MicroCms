using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>Returns entries that live in a specific folder (GAP-02).</summary>
public sealed class EntriesByFolderSpec : BaseSpecification<Entry>
{
    public EntriesByFolderSpec(FolderId folderId)
        : base(e => e.FolderId == folderId) { }
}
