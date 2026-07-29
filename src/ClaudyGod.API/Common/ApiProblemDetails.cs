using ClaudyGod.API.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Common;

/// <summary>
/// Single factory for every RFC 7807 response emitted by the HTTP layer.
/// Stable codes are intended for clients; correlation and trace identifiers
/// are intended for support and observability.
/// </summary>
public static class ApiProblemDetails
{
    public static ProblemDetails Create(
        HttpContext context,
        int status,
        string code,
        string title,
        string detail,
        IDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path,
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
            problem.Extensions["correlationId"] = correlationId;
        if (errors is not null)
            problem.Extensions["errors"] = errors;

        return problem;
    }
}
