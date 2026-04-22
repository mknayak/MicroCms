using Asp.Versioning;
using Hellang.Middleware.ProblemDetails;
using MicroCMS.Application;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

namespace MicroCMS.WebHost.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static WebApplicationBuilder AddLoggingAndTelemetry(this WebApplicationBuilder builder)
    {
      // TODO: Serilog + OpenTelemetry registration (Sprint 14)
        return builder;
    }

    internal static WebApplicationBuilder AddSecurityServices(this WebApplicationBuilder builder)
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
         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
};
   });

        builder.Services.AddAuthorization();

        builder.Services.AddCors(opt =>
            opt.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        builder.Services.AddRateLimiter(opt =>
        {
            opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
   opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
   {
        var tenantId = ctx.User?.FindFirst("tenant_id")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
          return RateLimitPartition.GetTokenBucketLimiter(tenantId, _ => new TokenBucketRateLimiterOptions
          {
       TokenLimit = 600,
   TokensPerPeriod = 600,
        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
    AutoReplenishment = true
       });
            });
     });

      return builder;
    }

    internal static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddApplication();
        return builder;
    }

    internal static WebApplicationBuilder AddInfrastructureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddInfrastructure(builder.Configuration);
        return builder;
    }

    internal static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
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
    Description = "Headless CMS REST API"
        });

   opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
     {
              Name = "Authorization",
          Type = SecuritySchemeType.Http,
     Scheme = "bearer",
                BearerFormat = "JWT",
          In = ParameterLocation.Header,
    Description = "Enter your JWT token."
            });

          opt.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
  {
    new OpenApiSecurityScheme
          {
    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
             },
          Array.Empty<string>()
  }
      });
        });

        // RFC 7807 Problem Details — map domain and application exceptions
        builder.Services.AddProblemDetails(opt =>
        {
    opt.IncludeExceptionDetails = (_, _) =>
           builder.Environment.IsDevelopment();

            opt.Map<NotFoundException>(ex =>
   new Hellang.Middleware.ProblemDetails.StatusCodeProblemDetails(StatusCodes.Status404NotFound)
  {
         Title = "Not Found",
         Detail = ex.Message
                });

  opt.Map<ConflictException>(ex =>
           new Hellang.Middleware.ProblemDetails.StatusCodeProblemDetails(StatusCodes.Status409Conflict)
           {
        Title = "Conflict",
         Detail = ex.Message
         });

            opt.Map<ValidationException>(ex =>
            {
     var pd = new Hellang.Middleware.ProblemDetails.StatusCodeProblemDetails(StatusCodes.Status422UnprocessableEntity)
          {
          Title = "Validation Failed",
       Detail = "One or more validation errors occurred."
     };
   pd.Extensions["errors"] = ex.Errors;
     return pd;
      });

   opt.Map<UnauthorizedException>(_ =>
       new Hellang.Middleware.ProblemDetails.StatusCodeProblemDetails(StatusCodes.Status401Unauthorized)
          {
         Title = "Unauthorized"
    });

            opt.Map<ForbiddenException>(ex =>
     new Hellang.Middleware.ProblemDetails.StatusCodeProblemDetails(StatusCodes.Status403Forbidden)
  {
         Title = "Forbidden",
          Detail = ex.Message
        });

         opt.Map<BusinessRuleViolationException>(ex =>
     new Hellang.Middleware.ProblemDetails.StatusCodeProblemDetails(StatusCodes.Status422UnprocessableEntity)
                {
                    Title = ex.RuleName,
             Detail = ex.Message
  });

      opt.Map<DomainException>(ex =>
         new Hellang.Middleware.ProblemDetails.StatusCodeProblemDetails(StatusCodes.Status422UnprocessableEntity)
      {
Title = "Domain Rule Violation",
    Detail = ex.Message
        });

       opt.Map<QuotaExceededException>(ex =>
     new Hellang.Middleware.ProblemDetails.StatusCodeProblemDetails(StatusCodes.Status429TooManyRequests)
                {
      Title = "Quota Exceeded",
      Detail = ex.Message
   });

            opt.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
  });

        return builder;
    }

    internal static WebApplicationBuilder AddGraphQlServices(this WebApplicationBuilder builder)
    {
        // TODO: Hot Chocolate dynamic schema (Sprint 9)
        return builder;
    }

  internal static WebApplicationBuilder AddPluginHosting(this WebApplicationBuilder builder)
    {
        // TODO: AssemblyLoadContext plugin loader (Sprint 11)
   return builder;
    }

    internal static WebApplicationBuilder AddAiServices(this WebApplicationBuilder builder)
    {
        // TODO: AI provider registry, orchestrator, budget service (Sprint 12)
        return builder;
    }

    internal static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
    return builder;
    }
}
