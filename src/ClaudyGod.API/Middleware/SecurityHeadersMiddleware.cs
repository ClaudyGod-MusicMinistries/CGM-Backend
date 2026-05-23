namespace ClaudyGod.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var h = ctx.Response.Headers;

        // Remove fingerprinting headers
        ctx.Response.OnStarting(() =>
        {
            h.Remove("X-Powered-By");
            h.Remove("Server");

            // Prevent MIME-type sniffing
            h["X-Content-Type-Options"] = "nosniff";

            // Deny iframe embedding (clickjacking)
            h["X-Frame-Options"] = "DENY";

            // Modern browsers: disable legacy XSS filter, rely on CSP
            h["X-XSS-Protection"] = "0";

            // Control referrer information
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Disable browser features not needed by this API
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=(), bluetooth=()";

            // API-safe CSP: this is a JSON API, no scripts/styles needed
            h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";

            // HSTS: also set by Traefik, but defence-in-depth if accessed directly
            if (ctx.Request.IsHttps)
                h["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";

            // Cache-Control for auth endpoints — never cache credentials
            if (ctx.Request.Path.StartsWithSegments("/api/v1.0/auth"))
                h["Cache-Control"] = "no-store, no-cache, must-revalidate";

            return Task.CompletedTask;
        });

        await _next(ctx);
    }
}
