namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>SiteTemplate</c> aggregates.</summary>
public readonly record struct SiteTemplateId(Guid Value)
{
    public static SiteTemplateId New()  => new(Guid.NewGuid());
    public static SiteTemplateId Empty  => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
