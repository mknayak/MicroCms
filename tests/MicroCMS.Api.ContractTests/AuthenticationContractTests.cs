using FluentAssertions;
using MicroCMS.Api.ContractTests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MicroCMS.Api.ContractTests;

/// <summary>
/// Sprint 5 security contract: verifies that unauthenticated requests are
/// rejected (401) for all protected endpoints, confirming JWT middleware is wired.
/// </summary>
public sealed class AuthenticationContractTests
{
    [Theory]
    [InlineData("/api/v1/entries?siteId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/v1/contenttypes?siteId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/v1/media?siteId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/v1/taxonomy/tags?siteId=00000000-0000-0000-0000-000000000001")]
 [InlineData("/api/v1/users")]
    [InlineData("/api/v1/admin/tenants")]
    public async Task ProtectedEndpoints_WithoutToken_Return401(string path)
    {
      await using var factory = new ApiWebApplicationFactory();
        // Use a bare client with no default headers (no bearer token)
  var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
     {
  AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

  response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
 because: $"endpoint {path} must require authentication");
    }
}
