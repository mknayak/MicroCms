namespace MicroCMS.Application.Features.Tenants.Dtos;

public sealed record TenantDto(
    Guid Id,
    string Slug,
    string DisplayName,
    string DefaultLocale,
    string TimeZoneId,
    bool AiEnabled,
    string? LogoUrl,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<SiteDto> Sites);

public sealed record SiteDto(
    Guid Id,
 string Name,
    string Handle,
    string DefaultLocale,
    bool IsActive,
    string? CustomDomain);

public sealed record TenantListItemDto(
    Guid Id,
    string Slug,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAt);
