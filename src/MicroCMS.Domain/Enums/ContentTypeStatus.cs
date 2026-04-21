namespace MicroCMS.Domain.Enums;

/// <summary>Lifecycle states of a content type schema.</summary>
public enum ContentTypeStatus
{
    /// <summary>Schema is being designed; no entries can be created yet.</summary>
    Draft = 0,

    /// <summary>Published and available for entry creation.</summary>
    Active = 1,

    /// <summary>No new entries can be created; existing entries remain accessible.</summary>
    Archived = 2
}
