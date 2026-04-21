using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.UnitTests.Fixtures;

/// <summary>Shared test fixture data to avoid repetition across test classes.</summary>
public static class DomainFixtures
{
    public static TenantId TenantId => TenantId.New();
    public static SiteId SiteId => SiteId.New();
    public static ContentTypeId ContentTypeId => ContentTypeId.New();
    public static UserId UserId => UserId.New();

    public static TenantSlug ValidSlug => TenantSlug.Create("acme-corp");
    public static Locale DefaultLocale => Locale.Create("en-US");
    public static Locale FrenchLocale => Locale.Create("fr-FR");

    public static TenantSettings DefaultSettings => TenantSettings.Create(
        displayName: "Acme Corp",
        defaultLocale: DefaultLocale,
        timeZoneId: "UTC");

    public static Slug EntrySlug => Slug.Create("my-first-post");
    public static EmailAddress ValidEmail => EmailAddress.Create("alice@example.com");
    public static PersonName ValidName => PersonName.Create("Alice Example");
}
