namespace MicroCMS.Shared.Results;

/// <summary>
/// Represents a typed, structured error returned by domain and application operations.
/// </summary>
// CA1716: 'Error' is a reserved keyword in VB - suppressed as this is a well-established pattern
#pragma warning disable CA1716
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
#pragma warning restore CA1716
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static Error Unexpected(string code, string message) =>
        new(code, message, ErrorType.Unexpected);
}

/// <summary>Categorises the nature of an <see cref="Error"/> for HTTP status mapping.</summary>
public enum ErrorType
{
    None        = 0,
    Failure     = 1,
    NotFound    = 2,
    Validation  = 3,
    Conflict    = 4,
    Unauthorized = 5,
    Forbidden   = 6,
    Unexpected  = 7,
}
