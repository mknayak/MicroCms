namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>ContentType</c> entities.</summary>
public readonly record struct ContentTypeId(Guid Value)
{
    public static ContentTypeId New() => new(Guid.NewGuid());
    public static ContentTypeId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static ContentTypeId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out ContentTypeId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new ContentTypeId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
