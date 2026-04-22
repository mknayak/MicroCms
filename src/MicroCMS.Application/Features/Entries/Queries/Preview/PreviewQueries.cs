using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MicroCMS.Application.Features.Entries.Queries.Preview;

/// <summary>
/// Generates a short-lived HMAC-SHA256 signed preview JWT for a draft entry (GAP-10).
/// The token is stateless — never stored in DB. Max lifetime: 1 hour.
/// </summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GeneratePreviewTokenQuery(
    Guid EntryId,
    TimeSpan? ExpiresIn = null) : IQuery<PreviewTokenResult>;

public sealed record PreviewTokenResult(string Token, DateTimeOffset ExpiresAt, string PreviewUrl);

/// <summary>
/// Fetches an entry by a stateless HMAC preview token — bypasses Published status check (GAP-10).
/// </summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GetEntryByPreviewTokenQuery(string Token) : IQuery<EntryDto>;

// ── Handlers ──────────────────────────────────────────────────────────────────

/// <summary>Handles <see cref="GeneratePreviewTokenQuery"/>.</summary>
public sealed class GeneratePreviewTokenQueryHandler(
    IRepository<Entry, EntryId> entryRepository,
    IPreviewSecretProvider secretProvider,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GeneratePreviewTokenQuery, Result<PreviewTokenResult>>
{
    private static readonly TimeSpan MaxLifetime = TimeSpan.FromHours(1);

  public async Task<Result<PreviewTokenResult>> Handle(
    GeneratePreviewTokenQuery request,
   CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
       ?? throw new NotFoundException(nameof(Entry), request.EntryId);

        var expiresIn = request.ExpiresIn is { } d && d <= MaxLifetime ? d : MaxLifetime;
        var expiresAt = dateTimeProvider.UtcNow.Add(expiresIn);
     var secret = await secretProvider.GetSiteSecretAsync(entry.SiteId.Value, cancellationToken);

     var payload = JsonSerializer.Serialize(new
    {
     eid = entry.Id.Value,
    sid = entry.SiteId.Value,
     exp = expiresAt.ToUnixTimeSeconds()
       });

        var token = Sign(payload, secret);
    var previewUrl = $"/preview/{Uri.EscapeDataString(token)}";

        return Result.Success(new PreviewTokenResult(token, expiresAt, previewUrl));
    }

    internal static string Sign(string payload, string secret)
    {
  var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
     var hmac = Convert.ToBase64String(HMACSHA256.HashData(key, data));
  var b64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        return $"{b64Payload}.{hmac}";
    }
}

/// <summary>Handles <see cref="GetEntryByPreviewTokenQuery"/>.</summary>
public sealed class GetEntryByPreviewTokenQueryHandler(
    IRepository<Entry, EntryId> entryRepository,
    IPreviewSecretProvider secretProvider,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetEntryByPreviewTokenQuery, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
     GetEntryByPreviewTokenQuery request,
        CancellationToken cancellationToken)
    {
        var parts = request.Token.Split('.');
  if (parts.Length != 2)
          return Result.Failure<EntryDto>(Error.Validation("Preview.InvalidToken", "Invalid preview token format."));

     string payloadJson;
        try { payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])); }
        catch { return Result.Failure<EntryDto>(Error.Validation("Preview.InvalidToken", "Invalid preview token encoding.")); }

 using var doc = JsonDocument.Parse(payloadJson);
     var root = doc.RootElement;

     if (!root.TryGetProperty("eid", out var eidEl) ||
   !root.TryGetProperty("sid", out var sidEl) ||
   !root.TryGetProperty("exp", out var expEl))
    return Result.Failure<EntryDto>(Error.Validation("Preview.InvalidToken", "Token payload missing required claims."));

     var exp = DateTimeOffset.FromUnixTimeSeconds(expEl.GetInt64());
  if (exp < dateTimeProvider.UtcNow)
   return Result.Failure<EntryDto>(Error.Validation("Preview.TokenExpired", "Preview token has expired."));

   var siteId = sidEl.GetGuid();
  var secret = await secretProvider.GetSiteSecretAsync(siteId, cancellationToken);
   var expected = GeneratePreviewTokenQueryHandler.Sign(payloadJson, secret);
   if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
 Encoding.UTF8.GetBytes(request.Token)))
   return Result.Failure<EntryDto>(Error.Validation("Preview.InvalidSignature", "Preview token signature is invalid."));

        var entryId = new EntryId(eidEl.GetGuid());
     var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
  ?? throw new NotFoundException(nameof(Entry), entryId);

       return Result.Success(EntryMapper.ToDto(entry));
 }
}

/// <summary>
/// Supplies the per-site HMAC secret used to sign and verify preview tokens (GAP-10).
/// Implementations read from SiteSettings or a secrets vault — never from entry FieldsJson.
/// </summary>
public interface IPreviewSecretProvider
{
    Task<string> GetSiteSecretAsync(Guid siteId, CancellationToken cancellationToken = default);
}
