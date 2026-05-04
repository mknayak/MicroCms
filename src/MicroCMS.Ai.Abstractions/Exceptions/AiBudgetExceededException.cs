namespace MicroCMS.Ai.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when AI budget limit is exceeded.
/// Maps to HTTP 429 Too Many Requests.
/// </summary>
public sealed class AiBudgetExceededException : Exception
{
    public AiBudgetExceededException(string message) : base(message)
    {
    }
}
