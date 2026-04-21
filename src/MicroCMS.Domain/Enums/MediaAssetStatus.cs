namespace MicroCMS.Domain.Enums;

/// <summary>Lifecycle states of a media asset.</summary>
public enum MediaAssetStatus
{
    /// <summary>Upload in progress; not yet complete.</summary>
    Uploading = 0,

    /// <summary>Upload complete; virus scan pending (FR-ML-4).</summary>
    PendingScan = 1,

    /// <summary>Passed virus scan; accessible via signed or public URL.</summary>
    Available = 2,

    /// <summary>Virus scan flagged the file; quarantined, inaccessible.</summary>
    Quarantined = 3,

    /// <summary>Soft-deleted.</summary>
    Deleted = 4
}
