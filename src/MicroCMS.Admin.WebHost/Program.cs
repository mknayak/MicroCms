// MicroCMS Admin WebHost — serves the React SPA and acts as a BFF (Backend-for-Frontend).
// In Development it proxies to the Vite dev server; in Production it serves the built dist/.

using MicroCMS.Admin.WebHost.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddAdminSecurityServices();
builder.AddAdminApplicationServices();
builder.AddInfrastructureServices();
builder.AddAdminSpaServices();
builder.AddAdminHealthChecks();

var app = builder.Build();

app.UseAdminSecurityMiddleware();
app.UseAdminSpa();

await app.RunAsync();

// Expose for WebApplicationFactory
public partial class Program { }
