namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>ConfigEntry</c> entities.</summary>
public readonly record struct ConfigEntryId(Guid Value)
{
    public static ConfigEntryId New() => new(Guid.NewGuid());
    public static ConfigEntryId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static ConfigEntryId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out ConfigEntryId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new ConfigEntryId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
