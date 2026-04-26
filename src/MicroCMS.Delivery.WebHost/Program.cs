// MicroCMS Delivery — composition root.
// Serves only published content; authenticated via X-Api-Key header.

using Asp.Versioning;
using Hellang.Middleware.ProblemDetails;
using MicroCMS.Application;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Delivery.Handlers;
using MicroCMS.Delivery.Core.Extensions;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// ── Serilog ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ── Application + Infrastructure layers ──────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Delivery Core: API key auth + component renderer ─────────────────────────
builder.Services.AddDeliveryServices();

// ── MVC + versioning ──────────────────────────────────────────────────────────
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(MicroCMS.Delivery.WebHost.Controllers.EntriesController).Assembly);

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

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
 opt.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
      Title = "MicroCMS Delivery API",
        Version = "v1",
        Description = "Read-only content delivery. Authenticate with X-Api-Key header.",
    });

    opt.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
     In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "API key issued from MicroCMS Admin → Sites → API Clients.",
    });

    opt.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
 {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
   {
    Reference = new Microsoft.OpenApi.Models.OpenApiReference
          {
         Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
          Id = "ApiKey",
},
            },
  Array.Empty<string>()
        },
    });
});

// ── Problem Details ───────────────────────────────────────────────────────────
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

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// ── Rate limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter
        .Create<HttpContext, string>(ctx =>
        {
         var key = ctx.User?.FindFirst("site_id")?.Value
   ?? ctx.Connection.RemoteIpAddress?.ToString()
                   ?? "anon";
       return System.Threading.RateLimiting.RateLimitPartition
      .GetTokenBucketLimiter(key, _ => new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
      {
     TokenLimit = 500,
     TokensPerPeriod = 500,
  ReplenishmentPeriod = TimeSpan.FromMinutes(1),
       QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
      QueueLimit = 20,
     });
    });
});

// ════════════════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ── Developer tools ───────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opt => opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Delivery API v1"));
}

app.UseProblemDetails();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

public partial class Program { }
