namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>Tag</c> entities.</summary>
public readonly record struct TagId(Guid Value)
{
    public static TagId New() => new(Guid.NewGuid());
    public static TagId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static TagId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out TagId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new TagId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
