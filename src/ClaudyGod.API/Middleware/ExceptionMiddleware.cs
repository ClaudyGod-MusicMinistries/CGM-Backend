using System.Net;
using System.Text.Json;
using ClaudyGod.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using ValidationException = ClaudyGod.Domain.Exceptions.ValidationException;

namespace ClaudyGod.API.Middleware;

/// <summary>
/// Translates unhandled exceptions into RFC 7807 ProblemDetails responses.
/// All error shapes are consistent: { type, title, status, detail, instance }
/// plus an optional "errors" extension for validation failures.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy  = JsonNamingPolicy.CamelCase,
    };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title, detail, errors) = Classify(ex);

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(ex, "Handled exception [{Status}] on {Method} {Path}",
                status, context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Type     = $"https://httpstatuses.io/{status}",
            Title    = title,
            Status   = status,
            Detail   = detail,
            Instance = context.Request.Path,
        };

        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cid))
            problem.Extensions["correlationId"] = cid;

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = status;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static (int status, string title, string detail, IDictionary<string, string[]>? errors)
        Classify(Exception ex) => ex switch
    {
        NotFoundException nfe =>
            (StatusCodes.Status404NotFound,
             "Resource Not Found",
             nfe.Message, null),

        DuplicateResourceException dre =>
            (StatusCodes.Status409Conflict,
             "Conflict",
             dre.Message, null),

        ValidationException ve =>
            (StatusCodes.Status422UnprocessableEntity,
             "Validation Failed",
             "One or more validation errors occurred. See 'errors' for details.",
             (IDictionary<string, string[]>)new Dictionary<string, string[]>(ve.Errors)),

        Application.Common.Exceptions.ValidationException ave =>
            (StatusCodes.Status422UnprocessableEntity,
             "Validation Failed",
             "One or more validation errors occurred. See 'errors' for details.",
             ave.Errors),

        ServiceUnavailableException sue =>
            (StatusCodes.Status503ServiceUnavailable,
             "Service Unavailable",
             sue.Message, null),

        DomainException de =>
            (StatusCodes.Status400BadRequest,
             "Business Rule Violation",
             de.Message, null),

        UnauthorizedAccessException =>
            (StatusCodes.Status401Unauthorized,
             "Unauthorized",
             "Authentication is required to access this resource.", null),

        _ =>
            (StatusCodes.Status500InternalServerError,
             "Internal Server Error",
             "An unexpected error occurred. Please try again later.", null),
    };
}
