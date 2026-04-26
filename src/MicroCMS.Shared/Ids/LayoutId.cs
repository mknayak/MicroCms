namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>Layout</c> aggregates.</summary>
public readonly record struct LayoutId(Guid Value)
{
    public static LayoutId New()   => new(Guid.NewGuid());
    public static LayoutId Empty   => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
