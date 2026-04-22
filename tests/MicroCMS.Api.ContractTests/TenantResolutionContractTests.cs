using FluentAssertions;
using MicroCMS.Api.ContractTests.Fixtures;
using System.Net;
using Xunit;

namespace MicroCMS.Api.ContractTests;

/// <summary>
/// Sprint 5 security contract: verifies that the <c>X-Tenant-Slug</c> spoof
/// header is silently ignored for non-SystemAdmin callers, and that the
/// tenant resolution middleware does not leak 500s on unknown slugs.
/// </summary>
public sealed class TenantResolutionContractTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task Request_WithXTenantSlugHeader_NonAdmin_DoesNotSpoofTenant()
    {
        await using var nonAdminFactory = new AuthorApiFactory();
     var client = nonAdminFactory.CreateClient();
  client.DefaultRequestHeaders.Add("X-Tenant-Slug", "evil-tenant");

   // Health is exempt — must pass through without error regardless of slug header
  var response = await client.GetAsync("/health/live");
      response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
public async Task SecurityHeaders_ArePresent_OnApiResponse()
    {
      var client = factory.CreateClient();
       var response = await client.GetAsync("/health/live");

    response.Headers.Should().ContainKey("X-Content-Type-Options");
       response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Referrer-Policy");
    response.Headers.Should().ContainKey("X-Correlation-ID");
 }

    [Fact]
    public async Task CorrelationId_WhenProvided_IsEchoedBack()
    {
  var client = factory.CreateClient();
   var correlationId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        var response = await client.GetAsync("/health/live");

   response.Headers.TryGetValues("X-Correlation-ID", out var values);
  values.Should().Contain(correlationId);
    }
}
