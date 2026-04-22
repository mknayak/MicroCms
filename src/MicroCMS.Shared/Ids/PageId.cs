using MicroCMS.Shared.Ids;

namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>Page</c> aggregates.</summary>
public readonly record struct PageId(Guid Value)
{
    public static PageId New() => new(Guid.NewGuid());
    public static PageId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
