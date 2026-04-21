namespace MicroCMS.Domain.Exceptions;

/// <summary>
/// Base class for all domain layer exceptions.
/// Thrown when a business rule or invariant is violated within an aggregate.
/// The application layer maps these to appropriate HTTP responses.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
