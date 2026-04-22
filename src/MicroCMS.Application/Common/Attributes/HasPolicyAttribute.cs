namespace MicroCMS.Application.Common.Attributes;

/// <summary>
/// Decorates a MediatR command or query to declare the authorization policy required to execute it.
/// <see cref="Behaviors.AuthorizationBehavior{TRequest,TResponse}"/> reads this attribute before
/// invoking the handler; absence of the attribute on a command type throws
/// <see cref="Exceptions.MissingPolicyException"/> at runtime (fail-secure default).
///
/// Usage:
/// <code>
/// [HasPolicy(ContentPolicies.EntryCreate)]
/// public record CreateEntryCommand(...) : IRequest&lt;Result&lt;EntryDto&gt;&gt;;
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class HasPolicyAttribute(string policy) : Attribute
{
    /// <summary>The policy name the caller must satisfy.</summary>
    public string Policy { get; } = policy;
}
