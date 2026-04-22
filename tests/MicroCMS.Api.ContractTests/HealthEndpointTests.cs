using FluentAssertions;
using MicroCMS.Api.ContractTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MicroCMS.Api.ContractTests;

/// <summary>
/// Verifies that the API health endpoints respond correctly without authentication.
/// </summary>
public sealed class HealthEndpointTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task LiveEndpoint_Returns200()
  {
  var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadyEndpoint_Returns200()
    {
   var client = factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
 response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
