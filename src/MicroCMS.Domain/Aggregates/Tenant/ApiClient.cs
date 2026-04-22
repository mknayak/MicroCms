using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Tenant;

/// <summary>Strongly-typed ID for <c>ApiClient</c>.</summary>
public readonly record struct ApiClientId(Guid Value)
{
    public static ApiClientId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Represents an API client key scoped to a site (GAP-20).
/// The raw key is generated once by the application layer and never stored here —
/// only the bcrypt hash is persisted (same pattern as GitHub PATs).
/// </summary>
public sealed class ApiClient : AggregateRoot<ApiClientId>
{
    public const int MaxNameLength = 200;

    private readonly List<string> _scopes = [];

    private ApiClient() : base() { } // EF Core

    private ApiClient(
  ApiClientId id,
        TenantId tenantId,
        SiteId siteId,
        string name,
        ApiKeyType keyType,
        string hashedSecret) : base(id)
    {
    TenantId = tenantId;
   SiteId = siteId;
        Name = name;
    KeyType = keyType;
        HashedSecret = hashedSecret;
     IsActive = true;
      CreatedAt = DateTimeOffset.UtcNow;
 }

    public TenantId TenantId { get; private set; }
  public SiteId SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ApiKeyType KeyType { get; private set; }
    /// <summary>bcrypt hash of the raw secret. Raw value shown exactly once on creation.</summary>
    public string HashedSecret { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<string> Scopes => _scopes.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

    public static ApiClient Create(
      TenantId tenantId,
   SiteId siteId,
        string name,
     ApiKeyType keyType,
 string hashedSecret,
        IEnumerable<string>? scopes = null,
   DateTimeOffset? expiresAt = null)
    {
     ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
      ArgumentException.ThrowIfNullOrWhiteSpace(hashedSecret, nameof(hashedSecret));

        if (name.Length > MaxNameLength)
    throw new DomainException($"API client name must not exceed {MaxNameLength} characters.");

        if (expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow)
 throw new DomainException("ExpiresAt must be in the future.");

        var client = new ApiClient(ApiClientId.New(), tenantId, siteId, name.Trim(), keyType, hashedSecret)
  {
   ExpiresAt = expiresAt
        };

        if (scopes is not null)
         client._scopes.AddRange(scopes.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct());

        return client;
    }

    // ── Mutations ─────────────────────────────────────────────────────────

    public void Revoke()
    {
  if (!IsActive)
       throw new BusinessRuleViolationException("ApiClient.AlreadyRevoked", "This API key has already been revoked.");
     IsActive = false;
    }

    public void RegenerateSecret(string newHashedSecret)
    {
    ArgumentException.ThrowIfNullOrWhiteSpace(newHashedSecret, nameof(newHashedSecret));
  HashedSecret = newHashedSecret;
     IsActive = true;
    }
}
