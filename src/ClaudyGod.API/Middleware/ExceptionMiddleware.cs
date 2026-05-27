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

    private static readonly JsonSerializerOptions _jsonOptions = new()
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        HttpStatusCode statusCode;
        string message;
        IEnumerable<string> errors = [];
        IDictionary<string, string[]> fieldErrors = new Dictionary<string, string[]>();

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

            // FluentValidation pipeline (via ValidationBehaviour → Domain.ValidationException)
            case ValidationException ve:
                statusCode = HttpStatusCode.UnprocessableEntity;
                message = "Validation failed. Please correct the highlighted fields.";
                fieldErrors = new Dictionary<string, string[]>(ve.Errors);
                errors = ve.Errors.SelectMany(e => e.Value);
                break;

            // Application-layer ValidationException (from AbstractValidator directly)
            case Application.Common.Exceptions.ValidationException ave:
                statusCode = HttpStatusCode.UnprocessableEntity;
                message = "Validation failed. Please correct the highlighted fields.";
                fieldErrors = ave.Errors;
                errors = ave.Errors.SelectMany(e => e.Value);
                break;

            case ServiceUnavailableException sue:
                statusCode = HttpStatusCode.ServiceUnavailable;
                message = sue.Message;
                break;

            case DomainException de:
                statusCode = HttpStatusCode.BadRequest;
                message = de.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "You are not authorized to perform this action.";
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred. Please try again.";
                break;
        }

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(ex, "Handled exception [{Status}]: {Message}",
                (int)statusCode, ex.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message, errors, fieldErrors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}
