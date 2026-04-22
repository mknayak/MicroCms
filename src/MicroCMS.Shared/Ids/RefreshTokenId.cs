namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for a <c>RefreshToken</c>.</summary>
public readonly record struct RefreshTokenId(Guid Value)
{
    public static RefreshTokenId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
