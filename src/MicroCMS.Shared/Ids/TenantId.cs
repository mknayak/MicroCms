namespace MicroCMS.Shared.Ids;

/// <summary>
/// Strongly-typed identifier for a Tenant. Wraps a <see cref="Guid"/> to prevent
/// accidental mix-up between different entity ID types at compile time.
/// </summary>
public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());
    public static TenantId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static TenantId Parse(string value) => new(Guid.Parse(value));
    public static bool TryParse(string value, out TenantId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new TenantId(guid);
            return true;
        }

        result = Empty;
        return false;
    }
}
