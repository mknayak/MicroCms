namespace MicroCMS.Domain.Exceptions;

/// <summary>
/// Thrown when an aggregate is asked to move to a state it cannot reach
/// from its current state (e.g. publishing an archived entry).
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string entityName, string from, string to)
        : base($"'{entityName}' cannot transition from '{from}' to '{to}'.")
    {
        EntityName = entityName;
        FromState = from;
        ToState = to;
    }

    public string EntityName { get; }
    public string FromState { get; }
    public string ToState { get; }
}
