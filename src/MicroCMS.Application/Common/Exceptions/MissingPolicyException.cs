namespace MicroCMS.Application.Common.Exceptions;

/// <summary>
/// Thrown when a command or query type is dispatched without a
/// <see cref="../Attributes/HasPolicyAttribute"/> decoration.
///
/// This is a programming error, not a runtime authorization failure.
/// Every command that mutates state MUST declare at least one policy — absence
/// of the attribute means the developer forgot to annotate the type (fail-secure default).
/// </summary>
public sealed class MissingPolicyException(Type requestType)
    : Exception(
        $"Request type '{requestType.FullName}' does not declare a [HasPolicy] attribute. " +
        $"All commands must be decorated with at least one [HasPolicy] to be dispatchable.")
{
    public Type RequestType { get; } = requestType;
}
