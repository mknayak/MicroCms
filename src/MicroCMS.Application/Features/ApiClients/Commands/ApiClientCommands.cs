using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Tenant;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.ApiClients.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record ApiClientDto(
    Guid Id,
 Guid SiteId,
    string Name,
    string KeyType,
    bool IsActive,
 IReadOnlyList<string> Scopes,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);

/// <summary>
/// Returned only on creation — raw key shown exactly once (GAP-20).
/// </summary>
public sealed record ApiClientCreatedDto(ApiClientDto Client, string RawKey);

// ── Commands ──────────────────────────────────────────────────────────────────

/// <summary>Creates an API client and returns the raw key once (GAP-20).</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record CreateApiClientCommand(
    Guid SiteId,
    string Name,
string KeyType,
  IReadOnlyList<string>? Scopes = null,
    DateTimeOffset? ExpiresAt = null) : ICommand<ApiClientCreatedDto>;

/// <summary>Revokes an API client key (GAP-20).</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record RevokeApiClientCommand(Guid ApiClientId) : ICommand<ApiClientDto>;

/// <summary>Regenerates an API client's secret and returns the new raw key once (GAP-20).</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record RegenerateApiClientCommand(Guid ApiClientId) : ICommand<ApiClientCreatedDto>;

// ── Queries ───────────────────────────────────────────────────────────────────

/// <summary>Lists all active API clients for a site.</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record ListApiClientsQuery(Guid SiteId) : IQuery<IReadOnlyList<ApiClientDto>>;

// ── Handlers ──────────────────────────────────────────────────────────────────

internal sealed class CreateApiClientCommandHandler(
    IRepository<ApiClient, ApiClientId> clientRepository,
    ICurrentUser currentUser,
    ISecretHasher hasher)
    : IRequestHandler<CreateApiClientCommand, Result<ApiClientCreatedDto>>
{
    public async Task<Result<ApiClientCreatedDto>> Handle(
        CreateApiClientCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ApiKeyType>(request.KeyType, ignoreCase: true, out var keyType))
          return Result.Failure<ApiClientCreatedDto>(
    Error.Validation("ApiClient.InvalidKeyType", $"'{request.KeyType}' is not a valid key type."));

     var rawKey = GenerateRawKey();
        var hashed = hasher.Hash(rawKey);
        var client = ApiClient.Create(
            currentUser.TenantId, new SiteId(request.SiteId),
  request.Name, keyType, hashed, request.Scopes, request.ExpiresAt);

        await clientRepository.AddAsync(client, cancellationToken);
      return Result.Success(new ApiClientCreatedDto(ApiClientMapper.ToDto(client), rawKey));
    }

    private static string GenerateRawKey() =>
        $"mck_{Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).TrimEnd('=')}";
}

internal sealed class RevokeApiClientCommandHandler(
    IRepository<ApiClient, ApiClientId> clientRepository)
    : IRequestHandler<RevokeApiClientCommand, Result<ApiClientDto>>
{
    public async Task<Result<ApiClientDto>> Handle(
       RevokeApiClientCommand request, CancellationToken cancellationToken)
    {
     var client = await clientRepository.GetByIdAsync(new ApiClientId(request.ApiClientId), cancellationToken)
    ?? throw new NotFoundException(nameof(ApiClient), request.ApiClientId);
  client.Revoke();
     clientRepository.Update(client);
        return Result.Success(ApiClientMapper.ToDto(client));
    }
}

internal sealed class RegenerateApiClientCommandHandler(
    IRepository<ApiClient, ApiClientId> clientRepository,
    ISecretHasher hasher)
    : IRequestHandler<RegenerateApiClientCommand, Result<ApiClientCreatedDto>>
{
    public async Task<Result<ApiClientCreatedDto>> Handle(
      RegenerateApiClientCommand request, CancellationToken cancellationToken)
    {
  var client = await clientRepository.GetByIdAsync(new ApiClientId(request.ApiClientId), cancellationToken)
      ?? throw new NotFoundException(nameof(ApiClient), request.ApiClientId);
        var rawKey = $"mck_{Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).TrimEnd('=')}";
     client.RegenerateSecret(hasher.Hash(rawKey));
        clientRepository.Update(client);
      return Result.Success(new ApiClientCreatedDto(ApiClientMapper.ToDto(client), rawKey));
    }
}

internal sealed class ListApiClientsQueryHandler(
    IRepository<ApiClient, ApiClientId> clientRepository)
    : IRequestHandler<ListApiClientsQuery, Result<IReadOnlyList<ApiClientDto>>>
{
    public async Task<Result<IReadOnlyList<ApiClientDto>>> Handle(
        ListApiClientsQuery request, CancellationToken cancellationToken)
    {
        var clients = await clientRepository.ListAsync(
   new ApiClientsBySiteSpec(new SiteId(request.SiteId)), cancellationToken);
  IReadOnlyList<ApiClientDto> result = clients.Select(ApiClientMapper.ToDto).ToList();
        return Result.Success(result);
    }
}

internal static class ApiClientMapper
{
    internal static ApiClientDto ToDto(ApiClient c) => new(
  c.Id.Value, c.SiteId.Value, c.Name, c.KeyType.ToString(),
   c.IsActive, c.Scopes, c.ExpiresAt, c.CreatedAt);
}

/// <summary>
/// Hashes API client secrets using SHA-256 (for lookup) or bcrypt (for storage).
/// Implementation lives in Infrastructure to keep crypto out of Application.
/// </summary>
public interface ISecretHasher
{
    /// <summary>Returns a hash of <paramref name="rawSecret"/> suitable for persistent storage.</summary>
    string Hash(string rawSecret);

    /// <summary>Verifies that <paramref name="rawSecret"/> matches <paramref name="storedHash"/>.</summary>
    bool Verify(string rawSecret, string storedHash);
}
