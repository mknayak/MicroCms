namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>Site</c> entities.</summary>
public readonly record struct SiteId(Guid Value)
{
    public static SiteId New() => new(Guid.NewGuid());
    public static SiteId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static SiteId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out SiteId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new SiteId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
