namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>User</c> entities.</summary>
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static UserId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out UserId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new UserId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
