namespace MicroCMS.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller is not authenticated. Mapped to HTTP 401 by the API middleware.
/// </summary>
public sealed class UnauthorizedException()
    : Exception("Authentication is required to perform this action.");
