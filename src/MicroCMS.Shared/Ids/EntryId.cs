namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>Entry</c> entities.</summary>
public readonly record struct EntryId(Guid Value)
{
    public static EntryId New() => new(Guid.NewGuid());
    public static EntryId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static EntryId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out EntryId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new EntryId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
