namespace MicroCMS.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource cannot be found.
/// Mapped to HTTP 404 by the API problem details middleware.
/// </summary>
public sealed class NotFoundException(string entityName, object key)
    : Exception($"Entity '{entityName}' with key '{key}' was not found.")
{
    public string EntityName { get; } = entityName;
    public object Key { get; } = key;
}
