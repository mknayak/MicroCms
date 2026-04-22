namespace MicroCMS.Application.Common.Exceptions;

/// <summary>
/// Thrown when a tenant has exceeded a resource quota.
/// Mapped to HTTP 429 (quota exhausted) by the Problem Details middleware.
/// </summary>
public sealed class QuotaExceededException(string quotaName, string detail)
    : Exception($"Quota '{quotaName}' exceeded: {detail}")
{
    public string QuotaName { get; } = quotaName;
}
