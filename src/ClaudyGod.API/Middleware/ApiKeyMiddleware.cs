using ClaudyGod.API.Attributes;
using System.Security.Cryptography;
using System.Text;

namespace ClaudyGod.API.Middleware;

/// <summary>
/// Validates API keys for endpoints not marked with [PublicEndpoint].
/// Requires x-api-key header with valid API key.
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly IConfiguration _config;

    // Framework-level infrastructure endpoints (not controller actions, so they can't
    // carry a [PublicEndpoint] attribute) that must remain reachable without an API key.
    private static readonly string[] InfrastructurePublicPaths = { "/health", "/healthz" };

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // CORS preflight requests never carry custom headers like x-api-key — let them
        // through so UseCors (which now runs before this middleware) can respond correctly.
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Check if this endpoint requires API key
        var path = context.Request.Path.Value ?? string.Empty;

        var isPublicEndpoint =
            InfrastructurePublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
            context.GetEndpoint()?.Metadata.GetMetadata<PublicEndpointAttribute>() is not null;

        if (!isPublicEndpoint)
        {
            // Get API key from header
            var apiKey = context.Request.Headers["x-api-key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Missing API key for endpoint: {Path}", path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Unauthorized access",
                    data = (object?)null,
                    errors = new[] { "Missing or invalid API key" },
                    fieldErrors = new Dictionary<string, string[]>()
                });
                return;
            }

            // Validate API key
            var validKeys = _config.GetSection("Security:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
            if (!validKeys.Any(key => FixedTimeEquals(key, apiKey)))
            {
                _logger.LogWarning("Invalid API key attempt for endpoint: {Path}. Key: {KeyLast4}", path, apiKey.Length > 4 ? apiKey[^4..] : "****");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Unauthorized access",
                    data = (object?)null,
                    errors = new[] { "Invalid API key" },
                    fieldErrors = new Dictionary<string, string[]>()
                });
                return;
            }

            // Store key info in context for logging
            context.Items["ApiKeyId"] = apiKey.Length > 4 ? apiKey[^4..] : "****";
        }

        await _next(context);
    }

    private static bool FixedTimeEquals(string configuredKey, string suppliedKey)
    {
        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedKey);

        return configuredBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(configuredBytes, suppliedBytes);
    }
}
