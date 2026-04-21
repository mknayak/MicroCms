namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>Category</c> entities.</summary>
public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.NewGuid());
    public static CategoryId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static CategoryId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out CategoryId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new CategoryId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
