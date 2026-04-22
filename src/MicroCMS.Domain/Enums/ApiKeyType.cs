namespace MicroCMS.Domain.Enums;

/// <summary>
/// Controls the access level of an API client key (GAP-20).
/// Delivery keys are read-only published; Management keys have full read/write;
/// Preview keys can read draft entries.
/// </summary>
public enum ApiKeyType
{
    Delivery = 0,
    Management = 1,
    Preview = 2
}
