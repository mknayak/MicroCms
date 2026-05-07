namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>EntryGroup</c> aggregates.</summary>
public readonly record struct EntryGroupId(Guid Value)
{
    public static EntryGroupId New() => new(Guid.NewGuid());
    public static EntryGroupId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static EntryGroupId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out EntryGroupId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new EntryGroupId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
