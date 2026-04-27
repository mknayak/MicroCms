namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for <c>EditLock</c> aggregates.</summary>
public readonly record struct EditLockId(Guid Value)
{
    public static EditLockId New()  => new(Guid.NewGuid());
    public static EditLockId Empty  => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
