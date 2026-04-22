namespace MicroCMS.Application.Common.Exceptions;

/// <summary>
/// Thrown when an entity cannot be created because a unique constraint would be violated.
/// Mapped to HTTP 409 by the API problem details middleware.
/// </summary>
public sealed class ConflictException(string entityName, object key)
    : Exception($"Entity '{entityName}' with key '{key}' already exists.")
{
    public string EntityName { get; } = entityName;
    public object Key { get; } = key;
}
