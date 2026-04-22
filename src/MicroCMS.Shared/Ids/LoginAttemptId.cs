namespace MicroCMS.Shared.Ids;

/// <summary>Strongly-typed identifier for a <c>LoginAttempt</c> audit record.</summary>
public readonly record struct LoginAttemptId(Guid Value)
{
    public static LoginAttemptId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
