namespace MicroCMS.Domain.Enums;

/// <summary>
/// Controls public/private visibility of a media asset (GAP-16).
/// Private assets require a signed URL with an expiry for delivery.
/// </summary>
public enum AssetVisibility
{
    Public = 0,
    Private = 1
}
