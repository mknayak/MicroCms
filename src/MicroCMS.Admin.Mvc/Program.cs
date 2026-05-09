// MicroCMS Admin MVC Host — serves a server-rendered Razor/MVC admin interface.
// Calls the MicroCMS backend API using a typed HttpClient with JWT forwarding.

using MicroCMS.Admin.Mvc.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddAdminMvcSecurity();
builder.AddAdminMvcServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseAdminMvcSecurity();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

await app.RunAsync();

// Expose for WebApplicationFactory
namespace MicroCMS.Admin.Mvc
{
    public partial class Program { }
}
