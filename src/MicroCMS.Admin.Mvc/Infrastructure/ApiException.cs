namespace MicroCMS.Admin.Mvc.Infrastructure;

/// <summary>
/// Represents an error response from the MicroCMS backend API.
/// Carries the HTTP status code and the RFC 7807 problem-detail payload.
/// </summary>
public sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public string? Detail { get; }

    public ApiException(int statusCode, string message, string? detail = null)
        : base(message)
    {
        StatusCode = statusCode;
        Detail = detail;
    }
}
