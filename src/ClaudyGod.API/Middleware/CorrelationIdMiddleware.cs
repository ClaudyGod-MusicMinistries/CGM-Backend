using Serilog.Context;

namespace ClaudyGod.API.Middleware;

/// <summary>
/// Reads or generates a correlation ID for every request and attaches it
/// to both the response headers and the Serilog log context so every log
/// line emitted during the request automatically includes the ID.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxCorrelationIdLength = 64;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var suppliedId = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsValid(suppliedId)
            ? suppliedId!
            : Guid.NewGuid().ToString("N")[..16];

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= MaxCorrelationIdLength &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
