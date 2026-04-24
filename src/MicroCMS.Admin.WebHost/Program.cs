// MicroCMS Admin WebHost — serves the React SPA and acts as a BFF (Backend-for-Frontend).
// In Development it proxies to the Vite dev server; in Production it serves the built dist/.

using MicroCMS.Admin.WebHost.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddAdminSecurityServices();
builder.AddAdminSpaServices();
builder.AddAdminHealthChecks();
builder.AddAdminApiProxy();

var app = builder.Build();

app.UseAdminSecurityMiddleware();
app.UseAdminApiProxy();
app.UseAdminSpa();

await app.RunAsync();

// Expose for WebApplicationFactory — scoped to avoid ambiguity with MicroCMS.WebHost.Program
namespace MicroCMS.Admin.WebHost
{
    public partial class Program { }
}
