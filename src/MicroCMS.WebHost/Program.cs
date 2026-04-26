// MicroCMS composition root.
// This file wires all layers together at startup.
// Individual extension methods live in Extensions/ to keep cyclomatic complexity low.

using MicroCMS.WebHost.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("Configuration/appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"Configuration/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ── Bridge TrustedClients:Admin → Jwt ────────────────────────────────────────
// JwtTokenService (Infrastructure) reads Jwt:Secret/Issuer/Audience/AccessTokenMinutes
// to mint tokens. WebHost validates via TrustedClients.  Rather than duplicating
// values in both sections, we derive Jwt:* from TrustedClients:Admin at startup so
// there is a single source of truth: TrustedClients.
var adminClient = builder.Configuration.GetSection("TrustedClients:Admin");
if (adminClient.Exists())
{
    var bridge = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
    {
      ["Jwt:Secret"]  = adminClient["Secret"],
        ["Jwt:Issuer"]  = adminClient["Issuer"],
 ["Jwt:Audience"] = adminClient["Audience"],
    };

  // Preserve AccessTokenMinutes if already set (e.g. from appsettings or env var);
    // fall back to the value already in config so we do not overwrite a legitimate override.
    if (string.IsNullOrEmpty(builder.Configuration["Jwt:AccessTokenMinutes"]))
   bridge["Jwt:AccessTokenMinutes"] = "15";

    builder.Configuration.AddInMemoryCollection(bridge);
}

// ─────────────────────────────────────────────────────────────────────────────

builder.AddLoggingAndTelemetry();
builder.AddSecurityServices();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddApiServices();
builder.AddGraphQlServices();
builder.AddPluginHosting();
builder.AddAiServices();
builder.AddHealthChecks();

var app = builder.Build();

await app.UseDatabaseAsync();

app.UseSecurityMiddleware();
app.UseApiMiddleware();
app.UseGraphQlMiddleware();
app.UseHealthCheckEndpoints();

await app.RunAsync();

// Expose the Program class for WebApplicationFactory in integration/E2E tests
public partial class Program { }
