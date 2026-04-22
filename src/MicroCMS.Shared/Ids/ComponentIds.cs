using MicroCMS.Shared.Ids;

namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>Component</c> aggregates (GAP-22).</summary>
public readonly record struct ComponentId(Guid Value)
{
    public static ComponentId New() => new(Guid.NewGuid());
    public static ComponentId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

/// <summary>Strongly-typed identifier for <c>ComponentItem</c> entities (GAP-22).</summary>
public readonly record struct ComponentItemId(Guid Value)
{
    public static ComponentItemId New() => new(Guid.NewGuid());
    public static ComponentItemId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}

/// <summary>Strongly-typed identifier for <c>PageTemplate</c> aggregates (GAP-23).</summary>
public readonly record struct PageTemplateId(Guid Value)
{
    public static PageTemplateId New() => new(Guid.NewGuid());
    public static PageTemplateId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
