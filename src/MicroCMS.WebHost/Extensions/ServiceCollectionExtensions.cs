using Asp.Versioning;
using Hellang.Middleware.ProblemDetails;
using MicroCMS.Application;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Security;
using MicroCMS.Domain.Exceptions;
using MicroCMS.GraphQL;
using MicroCMS.Infrastructure;
using MicroCMS.Delivery.Core.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;

namespace MicroCMS.WebHost.Extensions;

// ── Helpers ──────────────────────────────────────────────────────────────────

/// <summary>One entry from the <c>TrustedClients</c> configuration section.</summary>
internal sealed class TrustedClientOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scheme name used as the ASP.NET Core default.
    /// It reads the <c>iss</c> claim from the incoming Bearer token and
 /// forwards to the matching per-client scheme, so each client's tokens
    /// are validated against their own Secret/Issuer/Audience.
    /// </summary>
    private const string DispatchScheme = "JwtDispatch";

    // ── Logging / Telemetry ───────────────────────────────────────────────

    internal static WebApplicationBuilder AddLoggingAndTelemetry(
        this WebApplicationBuilder builder)
    {
        return builder;
    }

    // ── Security ──────────────────────────────────────────────────────────

    internal static WebApplicationBuilder AddSecurityServices(
     this WebApplicationBuilder builder)
    {
        var trustedClients = builder.Configuration
            .GetSection("TrustedClients")
            .GetChildren()
         .Select(s => (
   Name: s.Key,
    Scheme: $"Jwt_{s.Key}",
  Options: s.Get<TrustedClientOptions>() ?? new TrustedClientOptions()))
    .Where(c => !string.IsNullOrWhiteSpace(c.Options.Issuer))
        .ToList();

        if (trustedClients.Count == 0)
            throw new InvalidOperationException(
         "WebHost requires at least one entry under TrustedClients in configuration.");

        var issuerToScheme = trustedClients.ToDictionary(
            c => c.Options.Issuer,
            c => c.Scheme,
    StringComparer.OrdinalIgnoreCase);

  var authBuilder = builder.Services
        .AddAuthentication(DispatchScheme)
            .AddPolicyScheme(DispatchScheme, "JWT dispatch by issuer", opts =>
            {
    opts.ForwardDefaultSelector = ctx =>
      SelectScheme(ctx, issuerToScheme, trustedClients[0].Scheme);
      });

        foreach (var (_, scheme, options) in trustedClients)
        {
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
       authBuilder.AddJwtBearer(scheme, opt =>
            {
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
         ValidateAudience = true,
               ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
     ValidIssuer = options.Issuer,
      ValidAudience = options.Audience,
               IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.FromSeconds(30),
      };
       });
        }

        RegisterAuthorizationPolicies(builder);
        RegisterCorsAndRateLimiting(builder);

        return builder;
    }

    /// <summary>
    /// Reads the <c>iss</c> claim from the incoming Bearer token without full validation
    /// and returns the matching per-client JwtBearer scheme name.
    /// </summary>
    private static string SelectScheme(
    HttpContext ctx,
        Dictionary<string, string> issuerToScheme,
        string fallbackScheme)
 {
        var authHeader = ctx.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) != true)
     return fallbackScheme;

        var rawToken = authHeader["Bearer ".Length..].Trim();
        try
    {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(rawToken))
            {
           var jwt = handler.ReadJwtToken(rawToken);
       if (issuerToScheme.TryGetValue(jwt.Issuer, out var targetScheme))
          return targetScheme;
            }
 }
        catch { /* malformed token — fall through */ }

     return fallbackScheme;
    }

    private static void RegisterAuthorizationPolicies(WebApplicationBuilder builder)
    {
      builder.Services.AddAuthorization(opt =>
     {
            opt.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
  .RequireAuthenticatedUser()
   .Build();

            opt.AddPolicy(AuthorizationPolicies.TenantMember,   p => p.RequireAuthenticatedUser());
          opt.AddPolicy(AuthorizationPolicies.TenantAdmin,    p => p.RequireClaim("role", "TenantAdmin"));
   opt.AddPolicy(AuthorizationPolicies.ContentAuthor,  p => p.RequireClaim("role", "Author", "Editor", "Approver", "Publisher", "TenantAdmin"));
            opt.AddPolicy(AuthorizationPolicies.ContentEditor,  p => p.RequireClaim("role", "Editor", "Approver", "Publisher", "TenantAdmin"));
            opt.AddPolicy(AuthorizationPolicies.ContentApprover,p => p.RequireClaim("role", "Approver", "Publisher", "TenantAdmin"));
    opt.AddPolicy(AuthorizationPolicies.ContentPublisher,p => p.RequireClaim("role", "Publisher", "TenantAdmin"));
      opt.AddPolicy(AuthorizationPolicies.ApiKey,          p => p.RequireClaim("auth_method", "api_key"));
        });
    }

    private static void RegisterCorsAndRateLimiting(WebApplicationBuilder builder)
    {
    builder.Services.AddCors(opt =>
 opt.AddDefaultPolicy(policy =>
      policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        builder.Services.AddRateLimiter(opt =>
        {
 opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
   opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
         {
                var partitionKey = ctx.User?.FindFirst("tenant_id")?.Value
      ?? ctx.Connection.RemoteIpAddress?.ToString()
           ?? "anon";

    return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ =>
     new TokenBucketRateLimiterOptions
         {
      TokenLimit = 200,
         TokensPerPeriod = 200,
               ReplenishmentPeriod = TimeSpan.FromMinutes(1),
          QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
     QueueLimit = 10,
         });
      });
     });
    }

    // ── Application layer ─────────────────────────────────────────────────

    internal static WebApplicationBuilder AddApplicationServices(
        this WebApplicationBuilder builder)
    {
 builder.Services.AddApplication();
        return builder;
    }

    // ── Infrastructure layer ──────────────────────────────────────────────

    internal static WebApplicationBuilder AddInfrastructureServices(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddInfrastructure(builder.Configuration);
  builder.Services.AddDeliveryServices(setAsDefaultScheme: false);
 return builder;
    }

    // ── REST API layer ────────────────────────────────────────────────────

    internal static WebApplicationBuilder AddApiServices(
        this WebApplicationBuilder builder)
    {
        builder.Services
          .AddControllers()
            .AddApplicationPart(typeof(MicroCMS.Api.AssemblyReference).Assembly);

        builder.Services
            .AddApiVersioning(opt =>
        {
          opt.DefaultApiVersion = new ApiVersion(1, 0);
          opt.AssumeDefaultVersionWhenUnspecified = true;
     opt.ReportApiVersions = true;
        })
            .AddApiExplorer(opt =>
            {
   opt.GroupNameFormat = "'v'VVV";
       opt.SubstituteApiVersionInUrl = true;
      });

    builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(opt =>
        {
   opt.SwaggerDoc("v1", new OpenApiInfo
            {
         Title = "MicroCMS API",
             Version = "v1",
              Description = "Headless CMS REST API",
   });

         opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
           In = ParameterLocation.Header,
         Description = "Enter JWT token",
                Name = "Authorization",
        Type = SecuritySchemeType.Http,
    BearerFormat = "JWT",
                Scheme = "Bearer",
            });

            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
     {
             {
  new OpenApiSecurityScheme
   {
            Reference = new OpenApiReference
           {
    Type = ReferenceType.SecurityScheme,
         Id = "Bearer",
   },
           },
           Array.Empty<string>()
        },
     });
        });

 builder.Services.AddProblemDetails(opt =>
        {
    opt.MapToStatusCode<NotFoundException>(StatusCodes.Status404NotFound);
  opt.MapToStatusCode<ConflictException>(StatusCodes.Status409Conflict);
       opt.MapToStatusCode<ForbiddenException>(StatusCodes.Status403Forbidden);
            opt.MapToStatusCode<UnauthorizedException>(StatusCodes.Status401Unauthorized);
      opt.MapToStatusCode<ValidationException>(StatusCodes.Status422UnprocessableEntity);
    opt.MapToStatusCode<DomainException>(StatusCodes.Status400BadRequest);
            opt.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
    });

        return builder;
    }

    // ── GraphQL layer ─────────────────────────────────────────────────────

    internal static WebApplicationBuilder AddGraphQlServices(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddGraphQlSchema();
        return builder;
    }

    // ── Plugin hosting ────────────────────────────────────────────────────

    internal static WebApplicationBuilder AddPluginHosting(
        this WebApplicationBuilder builder)
    {
     return builder;
    }

    // ── AI services ───────────────────────────────────────────────────────

    internal static WebApplicationBuilder AddAiServices(
        this WebApplicationBuilder builder)
    {
        return builder;
    }

    // ── Health checks ─────────────────────────────────────────────────────

    internal static WebApplicationBuilder AddHealthChecks(
        this WebApplicationBuilder builder)
    {
        builder.Services
   .AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

     return builder;
    }
}
