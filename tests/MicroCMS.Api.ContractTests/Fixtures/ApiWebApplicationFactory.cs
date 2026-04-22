using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Shared.Ids;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace MicroCMS.Api.ContractTests.Fixtures;

/// <summary>
/// In-memory test server factory for HTTP contract tests.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public static readonly TenantId TestTenantId = TenantId.New();
  public static readonly Guid TestUserId = Guid.NewGuid();

  /// <summary>Roles granted to the default stub user. Override in subclasses to change permissions.</summary>
    protected virtual string[] StubRoles => ["SystemAdmin", "TenantAdmin"];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
    ArgumentNullException.ThrowIfNull(builder);
 builder.UseEnvironment("Testing");
   builder.ConfigureServices(services =>
        {
   var currentUser = Substitute.For<ICurrentUser>();
  currentUser.IsAuthenticated.Returns(true);
 currentUser.TenantId.Returns(TestTenantId);
    currentUser.UserId.Returns(TestUserId);
  currentUser.Email.Returns("admin@test.microcms.dev");
  currentUser.Roles.Returns(StubRoles);
 services.AddScoped<ICurrentUser>(_ => currentUser);

     var descriptor = services.SingleOrDefault(
      d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

     var dbName = $"contract_tests_{Guid.NewGuid():N}";
 services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlite($"DataSource={dbName};Mode=Memory;Cache=Shared"));
     });
    }
}

/// <summary>Factory variant that authenticates as a non-admin Author.</summary>
public sealed class AuthorApiFactory : ApiWebApplicationFactory
{
    protected override string[] StubRoles => ["Author"];
}
