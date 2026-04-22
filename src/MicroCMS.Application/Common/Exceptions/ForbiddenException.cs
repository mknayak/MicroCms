namespace MicroCMS.Application.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated caller lacks the required policy/permission.
/// Mapped to HTTP 403 by the API middleware.
/// </summary>
public sealed class ForbiddenException(string policy)
    : Exception($"Access denied. Required policy: '{policy}'.")
{
    public string Policy { get; } = policy;
}
