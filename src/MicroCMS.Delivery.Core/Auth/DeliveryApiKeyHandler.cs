using MicroCMS.Application.Features.ApiClients.Commands;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Tenant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MicroCMS.Delivery.Core.Auth;

/// <summary>
/// Authentication options for the delivery API key scheme.
/// </summary>
public sealed class DeliveryApiKeyOptions : AuthenticationSchemeOptions
{
    /// <summary>Name of the HTTP request header that carries the API key.</summary>
    public string HeaderName { get; set; } = DeliveryApiKeyDefaults.HeaderName;
}

/// <summary>Scheme name constants.</summary>
public static class DeliveryApiKeyDefaults
{
    public const string SchemeName = "DeliveryApiKey";
    public const string HeaderName = "X-Api-Key";
}

/// <summary>
/// Validates the <c>X-Api-Key</c> header against hashed secrets stored in <see cref="ApiClient"/>.
/// On success, builds a <see cref="ClaimsPrincipal"/> with tenant_id, site_id, and auth_method claims
/// matching the existing JWT claim naming convention used throughout the app.
/// </summary>
public sealed class DeliveryApiKeyHandler(
    IOptionsMonitor<DeliveryApiKeyOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<DeliveryApiKeyOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var rawKey) ||
     string.IsNullOrWhiteSpace(rawKey))
     {
            return AuthenticateResult.NoResult();
 }

        var secretHasher = Request.HttpContext.RequestServices
            .GetRequiredService<ISecretHasher>();
        var apiClientRepo = Request.HttpContext.RequestServices
  .GetRequiredService<IRepository<ApiClient, ApiClientId>>();

        var hashed = secretHasher.Hash(rawKey.ToString());
     var clients = await apiClientRepo.ListAsync(
 new ApiClientByHashSpec(hashed),
            Context.RequestAborted);

        var client = clients.FirstOrDefault(c =>
         c.IsActive &&
(c.ExpiresAt == null || c.ExpiresAt > DateTimeOffset.UtcNow));

        if (client is null)
            return AuthenticateResult.Fail("Invalid or revoked API key.");

      var claimsList = new List<Claim>
    {
            new("tenant_id",  client.TenantId.Value.ToString()),
      new("site_id",    client.SiteId.Value.ToString()),
            new("auth_method","api_key"),
          new("client_id",  client.Id.Value.ToString()),
      new(ClaimTypes.Name, client.Name),
        };

        foreach (var scope in client.Scopes)
        claimsList.Add(new Claim("scope", scope));

      var identity  = new ClaimsIdentity(claimsList, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
