using System.Text.Json;
using System.Runtime.ExceptionServices;
using ClaudyGod.API.Common;
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
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
    };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
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
            if (ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogDebug("Request cancelled by client on {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                return;
            }

            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(ex, "Unhandled exception after response started on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            ExceptionDispatchInfo.Capture(ex).Throw();
            return;
        }

        var (status, code, title, detail, errors) = Classify(ex);

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            _logger.LogInformation(
                "Request rejected Status={Status} Code={Code} ExceptionType={ExceptionType} Method={Method} Path={Path}",
                status, code, ex.GetType().Name, context.Request.Method, context.Request.Path);

        var problem = ApiProblemDetails.Create(context, status, code, title, detail, errors);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static (int status, string code, string title, string detail, IDictionary<string, string[]>? errors)
        Classify(Exception ex) => ex switch
        {
            NotFoundException nfe =>
                (StatusCodes.Status404NotFound, "RESOURCE_NOT_FOUND",
                 "Resource Not Found",
                 nfe.Message, null),

            DuplicateResourceException dre =>
                (StatusCodes.Status409Conflict, "ALREADY_EXISTS",
                 "Conflict",
                 dre.Message, null),

            ValidationException ve =>
                (StatusCodes.Status422UnprocessableEntity, "VALIDATION_FAILED",
                 "Validation Failed",
                 "One or more validation errors occurred. See 'errors' for details.",
                 (IDictionary<string, string[]>)new Dictionary<string, string[]>(ve.Errors)),

            Application.Common.Exceptions.ValidationException ave =>
                (StatusCodes.Status422UnprocessableEntity, "VALIDATION_FAILED",
                 "Validation Failed",
                 "One or more validation errors occurred. See 'errors' for details.",
                 ave.Errors),

            ServiceUnavailableException sue =>
                (StatusCodes.Status503ServiceUnavailable, "SERVICE_UNAVAILABLE",
                 "Service Unavailable",
                 sue.Message, null),

            DomainException de =>
                (StatusCodes.Status400BadRequest, "BUSINESS_RULE_VIOLATION",
                 "Business Rule Violation",
                 de.Message, null),

            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "AUTHENTICATION_REQUIRED",
                 "Unauthorized",
                 "Authentication is required to access this resource.", null),

            _ =>
                (StatusCodes.Status500InternalServerError, "UNEXPECTED_ERROR",
                 "Internal Server Error",
                 "An unexpected error occurred. Please try again later.", null),
        };
}
