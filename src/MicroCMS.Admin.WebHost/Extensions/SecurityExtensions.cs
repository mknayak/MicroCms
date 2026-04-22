using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MicroCMS.Admin.WebHost.Extensions;

/// <summary>
/// Registers JWT authentication, authorization, CORS, and HSTS/HTTPS-redirect
/// for the Admin BFF host.
/// </summary>
internal static class SecurityExtensions
{
    internal static WebApplicationBuilder AddAdminSecurityServices(this WebApplicationBuilder builder)
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured."));

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
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

        return builder;
    }

    internal static WebApplication UseAdminSecurityMiddleware(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors();

        app.Use(async (context, next) =>
        {
            string csp;

            if (app.Environment.IsDevelopment())
            {
                // Dev CSP allows:
                // - Vite dev server origin + HMR WebSocket (when npm run dev is active)
                // - ASP.NET hot-reload WebSocket (wss://localhost:44361/...)
                // - Visual Studio Browser Link (http://localhost:<random-port>)
                csp =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' http://localhost:5174; " +
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com http://localhost:5174; " +
                    "font-src 'self' https://fonts.gstatic.com; " +
                    "img-src 'self' data: blob: https:; " +
                    "connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*;";
            }
            else
            {
                csp =
                    "default-src 'self'; " +
                    "script-src 'self'; " +
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com; " +
                    "img-src 'self' data: blob: https:; " +
                    "connect-src 'self';";
            }

            context.Response.Headers.Append("Content-Security-Policy", csp);
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            await next();
        });

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
