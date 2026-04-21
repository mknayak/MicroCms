namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>Role</c> entities.</summary>
public readonly record struct RoleId(Guid Value)
{
    public static RoleId New() => new(Guid.NewGuid());
    public static RoleId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static RoleId Parse(string value) => new(Guid.Parse(value));

    public static bool TryParse(string value, out RoleId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new RoleId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
