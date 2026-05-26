using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using ClaudyGod.Application;
using ClaudyGod.Infrastructure;
using ClaudyGod.Infrastructure.Persistence;
using ClaudyGod.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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
                  .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With", "X-CSRF-Token")
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
        });

    builder.Services.AddAuthorization();

    // Redis distributed cache
    var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    var redisInstance = builder.Configuration["Redis:InstanceName"] ?? "claudygod:";
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = redisInstance;
    });

    // Health checks
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

    builder.Services.AddRateLimiter(options =>
    {
        // Global per-IP limiter
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        {
            var ip = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                     ?? ctx.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";
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
            var ip = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                     ?? ctx.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";
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
            var ip = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                     ?? ctx.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter($"auth:{ip}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        options.OnRejected = async (ctx, token) =>
        {
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ctx.HttpContext.Response.ContentType = "application/json";
            var retryAfter = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var r) ? (int)r.TotalSeconds : 60;
            ctx.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();
            var json = JsonSerializer.Serialize(new
            {
                success = false,
                message = $"Rate limit exceeded. Please retry after {retryAfter} seconds."
            });
            await ctx.HttpContext.Response.WriteAsync(json, token);
        };
    });

    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Swagger available in non-production environments only
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClaudyGod API v1"));
    }

    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<ExceptionMiddleware>();

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseCors("AllowFrontend");
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint
    app.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString().ToLower(),
                timestamp = DateTime.UtcNow,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString().ToLower(),
                    duration = e.Value.Duration.TotalMilliseconds
                })
            });
            await context.Response.WriteAsync(result);
        }
    });

    Log.Information("ClaudyGod API starting...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
