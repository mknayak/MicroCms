using MicroCMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Text.Json;

namespace MicroCMS.Infrastructure.Install;

/// <summary>
/// Short-circuits all requests that are not part of the install or health-check flow
/// while the system has not yet been installed.
///
/// Allowed paths (regardless of installation state):
///   GET  /api/v{n}/install/status
///   POST /api/v{n}/install
///   GET  /health/*
///   GET  /api-docs/*   (Swagger UI)
///   GET  /swagger/*    (Swagger JSON)
///
/// All other requests receive <c>503 Service Unavailable</c> with a JSON body that
/// explains how to complete the setup, until <see cref="IInstallationStateService"/>
/// reports the system is installed.
/// </summary>
public sealed class InstallationGuardMiddleware(
    RequestDelegate next,
    ILogger<InstallationGuardMiddleware> logger)
{
 // Pre-compiled prefix checks — avoids allocations on every request
    private static readonly string[] AllowedPrefixes =
    [
        "/health",
        "/api-docs",
        "/swagger",
    ];

    private static readonly string[] AllowedSegments =
    [
        "/install",   // matches /api/v1/install and /api/v1/install/status
    ];

    public async Task InvokeAsync(HttpContext context, IInstallationStateService installationState)
    {
   // Fast path — already installed, let every request through
 if (await installationState.IsInstalledAsync(context.RequestAborted))
      {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Allow install and health endpoints through unconditionally
        if (IsAllowedWhileUninstalled(path))
        {
         await next(context);
            return;
        }

        logger.LogWarning(
          "Request to {Path} blocked: system not yet installed.",
            context.Request.Path);

        await WriteNotInstalledResponseAsync(context);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static bool IsAllowedWhileUninstalled(string path)
    {
        foreach (var prefix in AllowedPrefixes)
        {
if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

      foreach (var segment in AllowedSegments)
        {
            if (path.Contains(segment, StringComparison.OrdinalIgnoreCase))
     return true;
   }

        return false;
    }

    private static async Task WriteNotInstalledResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = MediaTypeNames.Application.Json;
   context.Response.Headers["Retry-After"] = "0";

        var body = JsonSerializer.Serialize(new
        {
  status = 503,
        title = "System not installed",
      detail = "MicroCMS has not been set up yet. Complete the installation by sending a POST request to /api/v1/install.",
  installEndpoint = "/api/v1/install",
     statusEndpoint = "/api/v1/install/status",
        });

        await context.Response.WriteAsync(body, context.RequestAborted);
  }
}
