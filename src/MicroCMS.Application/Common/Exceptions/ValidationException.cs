using FluentValidation.Results;

namespace MicroCMS.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>ValidationBehavior</c> when one or more validators report errors.
/// Mapped to HTTP 422 by the API problem details middleware.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
