using Microsoft.AspNetCore.Authentication.Cookies;

namespace MicroCMS.Admin.Mvc.Extensions;

/// <summary>
/// Registers cookie-based authentication, CSRF protection, CORS, and security
/// headers for the Admin MVC host.
///
/// The MVC host uses cookie authentication so the browser session is managed
/// server-side. The JWT access token obtained from the API is stored as a claim
/// inside the encrypted cookie and forwarded on every outbound API call.
/// </summary>
internal static class SecurityExtensions
{
    internal static WebApplicationBuilder AddAdminMvcSecurity(this WebApplicationBuilder builder)
    {
        var cookieSection = builder.Configuration.GetSection("Cookie");
        var cookieName = cookieSection["Name"] ?? "mcms_admin_session";
        var expireMinutes = cookieSection.GetValue<int>("ExpireMinutes", 60);

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = cookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(expireMinutes);
                options.SlidingExpiration = true;
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

        builder.Services.AddAuthorization();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowed = builder.Configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? [];

                policy
                    .WithOrigins(allowed)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // Anti-forgery is added by default with AddControllersWithViews, but
        // we configure the cookie name explicitly for clarity.
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "mcms_csrf";
            options.Cookie.HttpOnly = false; // Must be readable by JS if needed
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        return builder;
    }

    internal static WebApplication UseAdminMvcSecurity(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors();

        app.Use(async (context, next) =>
        {
            AddSecurityHeaders(context, app.Environment.IsDevelopment());
            await next();
        });

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    private static void AddSecurityHeaders(HttpContext context, bool isDevelopment)
    {
        var csp = isDevelopment
            ? BuildDevelopmentCsp()
            : BuildProductionCsp();

        context.Response.Headers.Append("Content-Security-Policy", csp);
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    }

    private static string BuildDevelopmentCsp() =>
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: blob: https:; " +
        "connect-src 'self' http://localhost:* https://localhost:*;";

    private static string BuildProductionCsp() =>
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: blob: https:; " +
        "connect-src 'self';";
}
