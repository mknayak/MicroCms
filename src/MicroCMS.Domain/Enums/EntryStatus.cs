namespace MicroCMS.Domain.Enums;

/// <summary>Lifecycle states of a content entry.</summary>
public enum EntryStatus
{
    /// <summary>Work in progress — not visible to API consumers.</summary>
    Draft = 0,

    /// <summary>Submitted for editorial review.</summary>
    PendingReview = 1,

    /// <summary>Approved but not yet published.</summary>
    Approved = 2,

    /// <summary>Live and accessible via the Headless API.</summary>
    Published = 3,

    /// <summary>Was published, now taken offline.</summary>
    Unpublished = 4,

    /// <summary>Soft-deleted; excluded from all queries unless explicitly requested.</summary>
    Archived = 5,

    /// <summary>Awaiting scheduled publish datetime.</summary>
    Scheduled = 6
}
