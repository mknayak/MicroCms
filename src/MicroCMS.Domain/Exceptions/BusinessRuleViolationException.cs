namespace MicroCMS.Domain.Exceptions;

/// <summary>
/// Thrown when a domain operation violates a named business rule.
/// Carries the rule name to enable precise error reporting at the API layer.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string ruleName, string message)
     : base($"[{ruleName}] {message}")
    {
        RuleName = ruleName;
    }

    /// <summary>Machine-readable rule identifier, e.g. "Entry.CannotPublishWithoutApproval".</summary>
    public string RuleName { get; }
}
