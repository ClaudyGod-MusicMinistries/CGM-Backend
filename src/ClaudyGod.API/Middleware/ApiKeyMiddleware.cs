namespace ClaudyGod.API.Middleware;

/// <summary>
/// Validates API keys for public endpoints
/// Requires x-api-key header with valid API key
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly IConfiguration _config;

    // Endpoints that don't require API key authentication
    private static readonly string[] PublicEndpoints = new[]
    {
        "/health",
        "/healthz",
        "/api/v1.0/auth",
        "/api/v1.0/ai",  // Public chatbot and prayer features
    };

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this endpoint requires API key
        var path = context.Request.Path.Value ?? string.Empty;

        var isPublicEndpoint = PublicEndpoints.Any(ep => path.StartsWith(ep, StringComparison.OrdinalIgnoreCase));

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
            if (!validKeys.Contains(apiKey))
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
}
