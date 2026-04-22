namespace MicroCMS.Application.Features.Media.Dtos;

/// <summary>Carries a time-limited signed delivery URL for a private media asset.</summary>
public sealed record SignedUrlDto(
    Guid AssetId,
    string Url,
    DateTimeOffset ExpiresAt);
