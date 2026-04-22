using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MicroCMS.Infrastructure.Persistence.Common;

/// <summary>
/// Design-time factory used by the <c>dotnet ef</c> tool when generating migrations.
/// Reads the connection string from <c>appsettings.Development.json</c> (SQLite by default).
/// Not used at runtime.
/// </summary>
internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Walk up from the Infrastructure project to find the WebHost appsettings
        var basePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "MicroCMS.WebHost");

        if (!Directory.Exists(basePath))
        {
            basePath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=microcms_dev.db";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Default to SQLite for design-time (dev) scenarios
        optionsBuilder.UseSqlite(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
