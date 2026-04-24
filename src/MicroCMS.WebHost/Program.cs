// MicroCMS composition root.
// This file wires all layers together at startup.
// Individual extension methods live in Extensions/ to keep cyclomatic complexity low.

using MicroCMS.WebHost.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("Configuration/appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"Configuration/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

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
