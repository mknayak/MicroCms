using Asp.Versioning;
using Hellang.Middleware.ProblemDetails;
using MicroCMS.Application;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Security;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

namespace MicroCMS.WebHost.Extensions;

/// <summary>
/// Builder-level DI extension methods for the MicroCMS composition root.
/// Each method handles one vertical concern; cyclomatic complexity per method stays low.
/// </summary>
internal static class ServiceCollectionExtensions
{
    // ── Logging / Telemetry ───────────────────────────────────────────────

    internal static WebApplicationBuilder AddLoggingAndTelemetry(
        this WebApplicationBuilder builder)
    {
        // TODO Sprint 14 – Serilog structured logging + OpenTelemetry traces/metrics
        // builder.Host.UseSerilog(...);
        // builder.Services.AddOpenTelemetry()...;
        return builder;
    }

    // ── Security ──────────────────────────────────────────────────────────

    internal static WebApplicationBuilder AddSecurityServices(
        this WebApplicationBuilder builder)
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"] ?? "dev-secret-change-in-production-min32chars!!";
        var issuer = jwtSection["Issuer"] ?? "microcms";
        var audience = jwtSection["Audience"] ?? "microcms-api";

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        builder.Services.AddAuthorization(opt =>
        {
            // All JWT-authenticated users must pass the TenantMember baseline
            opt.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Content workflow policies — role claim checks
            opt.AddPolicy(AuthorizationPolicies.TenantMember,
                p => p.RequireAuthenticatedUser());

            opt.AddPolicy(AuthorizationPolicies.TenantAdmin,
                p => p.RequireClaim("role", "TenantAdmin"));

            opt.AddPolicy(AuthorizationPolicies.ContentAuthor,
                p => p.RequireClaim("role", "Author", "Editor", "Approver", "Publisher", "TenantAdmin"));

            opt.AddPolicy(AuthorizationPolicies.ContentEditor,
                p => p.RequireClaim("role", "Editor", "Approver", "Publisher", "TenantAdmin"));

            opt.AddPolicy(AuthorizationPolicies.ContentApprover,
                p => p.RequireClaim("role", "Approver", "Publisher", "TenantAdmin"));

            opt.AddPolicy(AuthorizationPolicies.ContentPublisher,
                p => p.RequireClaim("role", "Publisher", "TenantAdmin"));

            // API key policy — authenticated via API key scheme (header X-Api-Key)
            opt.AddPolicy(AuthorizationPolicies.ApiKey,
                p => p.RequireClaim("auth_method", "api_key"));
        });

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

        return builder;
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
        // TODO Sprint 9 – wire Hot Chocolate schema, query/mutation types, and data loaders
        // builder.Services.AddGraphQLServer()
        //     .AddQueryType<Query>()
        //     .AddMutationType<Mutation>()
        //     .AddFiltering()
        //     .AddSorting()
        //     .AddProjections()
        //     .AddAuthorization();
        return builder;
    }

    // ── Plugin hosting ────────────────────────────────────────────────────

    internal static WebApplicationBuilder AddPluginHosting(
        this WebApplicationBuilder builder)
    {
        // TODO Sprint 10 – load plugin manifests from the configured directory,
        // create AssemblyLoadContexts, call IPlugin.ConfigureServices for each.
        return builder;
    }

    // ── AI services ───────────────────────────────────────────────────────

    internal static WebApplicationBuilder AddAiServices(
        this WebApplicationBuilder builder)
    {
        // TODO Sprint 11 – register ILlmService routing, provider factories,
        // usage tracker, and budget enforcer from MicroCMS.Ai.Core.
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
