using ClaudyGod.API.Common;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace ClaudyGod.API.Middleware;

public sealed class DistributedRateLimitMiddleware(RequestDelegate next, ILogger<DistributedRateLimitMiddleware> logger)
{
    private static readonly LuaScript Script = LuaScript.Prepare("""
        local count = redis.call('INCR', @key)
        if count == 1 then redis.call('PEXPIRE', @key, @window) end
        return { count, redis.call('PTTL', @key) }
        """);

    private static readonly IReadOnlyDictionary<string, (int Limit, int Seconds)> Policies =
        new Dictionary<string, (int, int)>(StringComparer.Ordinal)
        {
            ["ai"] = (10, 60), ["auth"] = (10, 300), ["comments"] = (8, 600),
            ["subscription"] = (5, 3600), ["public-form"] = (10, 600), ["commerce"] = (5, 300)
        };

    public async Task InvokeAsync(HttpContext context, IConnectionMultiplexer redis, IConfiguration configuration)
    {
        if (context.Request.Method == HttpMethods.Options ||
            context.GetEndpoint()?.Metadata.GetMetadata<DisableRateLimitingAttribute>() is not null)
        {
            await next(context); return;
        }

        var global = (configuration.GetValue("RateLimit:PermitLimit", 100),
            configuration.GetValue("RateLimit:WindowSeconds", 60));
        var policyName = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
        var limits = policyName is not null && Policies.TryGetValue(policyName, out var named) ? named : global;
        var keyName = policyName ?? "global";
        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            var db = redis.GetDatabase();
            var key = $"claudygod:ratelimit:{keyName}:{client}";
            var result = (RedisResult[])(await db.ScriptEvaluateAsync(Script,
                new { key = (RedisKey)key, window = limits.Item2 * 1000 }))!;
            var count = (long)result[0];
            var ttl = Math.Max(1, (long)result[1] / 1000);
            if (count > limits.Item1)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers.RetryAfter = ttl.ToString();
                await context.Response.WriteAsJsonAsync(ApiProblemDetails.Create(context, 429,
                    "TOO_MANY_REQUESTS", "Please wait before trying again",
                    $"You have made too many requests. Please try again in about {ttl} seconds."));
                return;
            }
        }
        catch (RedisException ex)
        {
            // The local ASP.NET limiter remains active, preserving protection while
            // telemetry makes this distributed-protection degradation visible.
            logger.LogError(ex, "Distributed rate limiting unavailable; local limiter is active.");
        }

        await next(context);
    }
}
