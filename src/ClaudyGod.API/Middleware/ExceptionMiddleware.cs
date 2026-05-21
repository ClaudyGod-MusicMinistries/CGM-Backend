using System.Net;
using System.Text.Json;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Domain.Exceptions;
using ValidationException = ClaudyGod.Domain.Exceptions.ValidationException;

namespace ClaudyGod.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        HttpStatusCode statusCode;
        string message;
        IEnumerable<string>? errors = null;

        switch (ex)
        {
            case NotFoundException nfe:
                statusCode = HttpStatusCode.NotFound;
                message = nfe.Message;
                break;
            case DuplicateResourceException dre:
                statusCode = HttpStatusCode.Conflict;
                message = dre.Message;
                break;
            case ValidationException ve:
                statusCode = HttpStatusCode.BadRequest;
                message = "Validation failed.";
                errors = ve.Errors.SelectMany(e => e.Value);
                break;
            case Application.Common.Exceptions.ValidationException ave:
                statusCode = HttpStatusCode.BadRequest;
                message = "Validation failed.";
                errors = ave.Errors.SelectMany(e => e.Value);
                break;
            case DomainException de:
                statusCode = HttpStatusCode.BadRequest;
                message = de.Message;
                break;
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "Unauthorized.";
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred.";
                break;
        }

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception");
        else
            _logger.LogWarning(ex, "Handled exception: {Message}", ex.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message, errors);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
