using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using ClaudyGod.Application;
using ClaudyGod.Infrastructure;
using ClaudyGod.Infrastructure.Persistence;
using ClaudyGod.API.Middleware;
using ClaudyGod.API.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    // Handle migration-only mode (used by docker-compose migrate service)
    if (args.Contains("--migrate"))
    {
        try
        {
            var migrateHost = Host.CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseNpgsql(ctx.Configuration.GetConnectionString("DefaultConnection")));
                })
                .Build();

            using var scope = migrateHost.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Log.Information("Applying database migrations...");
            await db.Database.MigrateAsync();
            Log.Information("✓ Migrations applied successfully.");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "✗ Migration failed — connection string or DB issue");
            Environment.Exit(1);
        }
    }

    var builder = WebApplication.CreateBuilder(args);

    static bool IsPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        value.StartsWith("CHANGE-ME", StringComparison.OrdinalIgnoreCase);

    if (builder.Environment.IsProduction())
    {
        if (IsPlaceholder(builder.Configuration.GetConnectionString("DefaultConnection")) ||
            builder.Configuration.GetConnectionString("DefaultConnection")!.Contains("Password=CHANGE-ME", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A production database connection string must be configured.");
        if (IsPlaceholder(builder.Configuration["Jwt:Key"]))
            throw new InvalidOperationException("A production JWT signing key must be configured.");
        if (IsPlaceholder(builder.Configuration["Jwt:Issuer"]) || IsPlaceholder(builder.Configuration["Jwt:Audience"]))
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured in production.");
    }

    var configuredApiKeys = builder.Configuration.GetSection("Security:ApiKeys").Get<string[]>()
        ?.Where(key => !string.IsNullOrWhiteSpace(key))
        .ToArray() ?? [];
    if (builder.Environment.IsProduction() && configuredApiKeys.Length == 0)
        throw new InvalidOperationException("At least one Security:ApiKeys entry is required in production.");
    if (configuredApiKeys.Any(key => Encoding.UTF8.GetByteCount(key) < 32))
        throw new InvalidOperationException("Every Security:ApiKeys entry must be at least 32 bytes.");

    // Serilog
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

    // Application + Infrastructure layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Controllers
    builder.Services.AddControllers(options => options.Filters.Add<PaginationValidationFilter>())
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        // ExceptionMiddleware translates every thrown exception (FluentValidation
        // failures, NotFoundException, etc.) into an RFC 7807 ProblemDetails body
        // with a correlationId and an "errors" field-name -> messages map — that's
        // this API's one true error shape, and lib/data/client.ts on the frontend
        // already knows how to parse exactly that. [ApiController]'s automatic
        // model-validation failures (e.g. an unparsable query enum) bypass the
        // exception pipeline entirely, so without this override they'd return
        // ASP.NET's default ValidationProblemDetails instead — same content type,
        // but a different property set (no correlationId/instance) than every
        // other error response. Build the identical shape here for consistency.
        options.InvalidModelStateResponseFactory = context =>
        {
            var fieldErrors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var problem = new ProblemDetails
            {
                Type = "https://httpstatuses.io/400",
                Title = "Please check the information you entered",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Some information was missing or invalid. Correct the highlighted fields and try again.",
                Instance = context.HttpContext.Request.Path,
            };
            if (context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cid))
                problem.Extensions["correlationId"] = cid;
            problem.Extensions["errors"] = fieldErrors;
            problem.Extensions["code"] = "INVALID_REQUEST";

            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" },
            };
        };
    });
    builder.Services.AddEndpointsApiExplorer();

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Swagger (enabled in all environments for self-documenting API)
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "ClaudyGod Ministry API", Version = "v1", Description = "Production API for ClaudyGod Ministry" });
        c.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter your JWT token"
        });
        c.AddSecurityRequirement(new()
        {
            {
                new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    // CORS — origins must be explicitly configured; no insecure localhost fallback in production
    var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();
    if (allowedOrigins is null || allowedOrigins.Length == 0)
    {
        if (builder.Environment.IsProduction())
            throw new InvalidOperationException("Cors:Origins must be configured for production.");
        allowedOrigins = ["http://localhost:3000", "http://localhost:3001"];
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
            policy.WithOrigins(allowedOrigins)
                  .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With", "X-CSRF-Token", "X-API-Key")
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
    });

    // JWT Authentication
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key is required.");

    if (System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
        throw new InvalidOperationException("Jwt:Key must be at least 32 bytes for HmacSha256.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Type = "https://httpstatuses.io/401",
                        Title = "Unauthorized",
                        Status = StatusCodes.Status401Unauthorized,
                        Detail = "A valid bearer token is required.",
                        Instance = context.Request.Path,
                    });
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Type = "https://httpstatuses.io/403",
                        Title = "Forbidden",
                        Status = StatusCodes.Status403Forbidden,
                        Detail = "The authenticated identity does not have permission to access this resource.",
                        Instance = context.Request.Path,
                    });
                },
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // Secure-by-default: any endpoint not explicitly marked [AllowAnonymous]
        // is an administrative endpoint and requires an authenticated admin role.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole("Admin", "SuperAdmin")
            .Build();
    });

    // Forwarded headers are accepted only from explicitly trusted proxies.
    // Without this allow-list, client-supplied X-Forwarded-For values are ignored.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var value in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
        {
            if (!System.Net.IPAddress.TryParse(value, out var address))
                throw new InvalidOperationException($"ReverseProxy:KnownProxies contains invalid IP address '{value}'.");
            options.KnownProxies.Add(address);
        }
    });

    // Redis distributed cache
    var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    var redisInstance = builder.Configuration["Redis:InstanceName"] ?? "claudygod:";
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = redisInstance;
    });

    // PostgreSQL is required for readiness; Redis is an optional cache and may
    // degrade without removing an otherwise functional instance from service.
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["db"])
        .AddRedis(
            redisConn,
            name: "redis",
            failureStatus: HealthStatus.Degraded,
            tags: ["cache"]);

    // Rate Limiting
    var permitLimit = builder.Configuration.GetValue<int>("RateLimit:PermitLimit", 100);
    var windowSeconds = builder.Configuration.GetValue<int>("RateLimit:WindowSeconds", 60);

    static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    builder.Services.AddRateLimiter(options =>
    {
        // Global per-IP limiter
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        {
            var ip = ClientKey(ctx);
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
        });

        // Strict limiter for AI endpoints — 10 requests/minute per IP
        options.AddPolicy("ai", ctx =>
        {
            var ip = ClientKey(ctx);
            return RateLimitPartition.GetFixedWindowLimiter($"ai:{ip}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        // Strict limiter for auth endpoints — 10 attempts/5 minutes per IP
        options.AddPolicy("auth", ctx =>
        {
            var ip = ClientKey(ctx);
            return RateLimitPartition.GetFixedWindowLimiter($"auth:{ip}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        // Comment/like endpoints — anonymous, public, no login gate, so this is
        // the main spam/abuse defense besides the honeypot field on create.
        // 8 comments/10 minutes per IP is generous for a real visitor, tight
        // enough to blunt a scripted burst.
        options.AddPolicy("comments", ctx =>
        {
            var ip = ClientKey(ctx);
            return RateLimitPartition.GetFixedWindowLimiter($"comments:{ip}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        options.AddPolicy("subscription", ctx =>
        {
            var ip = ClientKey(ctx);
            return RateLimitPartition.GetFixedWindowLimiter($"subscription:{ip}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        options.OnRejected = async (ctx, token) =>
        {
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ctx.HttpContext.Response.ContentType = "application/problem+json";
            var retryAfter = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var r) ? (int)r.TotalSeconds : 60;
            ctx.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();
            var problem = new ProblemDetails
            {
                Type = "https://httpstatuses.io/429",
                Title = "Too Many Requests",
                Status = StatusCodes.Status429TooManyRequests,
                Detail = "You have made too many requests. Please wait a little while and try again.",
                Instance = ctx.HttpContext.Request.Path,
            };
            problem.Extensions["code"] = "TOO_MANY_REQUESTS";
            problem.Extensions["retryAfterSeconds"] = retryAfter;
            var json = JsonSerializer.Serialize(problem);
            await ctx.HttpContext.Response.WriteAsync(json, token);
        };
    });

    builder.Services.AddHttpContextAccessor();

    // ProblemDetails (RFC 7807) — enables consistent machine-readable error responses
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // Must run before anything reads scheme or client IP. Only configured
    // KnownProxies are trusted by ForwardedHeadersOptions above.
    app.UseForwardedHeaders();

    // Correlation ID must be first so every subsequent middleware and log has the ID
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            if (ctx.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cid))
                diag.Set("CorrelationId", cid);
        };
    });

    // Swagger available in non-production environments only
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClaudyGod API v1"));
    }

    // Routing must be established before CORS/ApiKeyMiddleware so that:
    //  (a) UseCors can short-circuit preflight OPTIONS requests with proper headers, and
    //  (b) ApiKeyMiddleware can read endpoint metadata (e.g. [PublicEndpoint]).
    app.UseRouting();

    // CORS must run before ApiKeyMiddleware. Previously it ran after, so a CORS preflight
    // (OPTIONS) request — which never carries the custom x-api-key header — was rejected
    // with a bare 401 by ApiKeyMiddleware before Access-Control-Allow-Origin was ever
    // attached, which the browser reports as a CORS failure instead of the real 401.
    app.UseCors("AllowFrontend");

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<ApiKeyMiddleware>();

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    static async Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString().ToLowerInvariant(),
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }

    // Liveness answers only whether the process can serve HTTP. Readiness
    // includes dependencies and returns 503 when PostgreSQL is unavailable.
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = WriteHealthResponse,
    }).AllowAnonymous();
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        ResponseWriter = WriteHealthResponse,
    }).AllowAnonymous();
    app.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        ResponseWriter = WriteHealthResponse,
    }).AllowAnonymous();

    Log.Information("ClaudyGod API starting...");
    await app.RunAsync();
}
catch (HostAbortedException)
{
    // Expected control flow used by EF tooling and WebApplicationFactory while
    // resolving the application's service provider. It must not be swallowed.
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
