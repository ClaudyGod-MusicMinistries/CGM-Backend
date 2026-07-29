using ClaudyGod.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClaudyGod.API.Filters;

/// <summary>
/// Enforces one pagination contract across every controller. Query handlers
/// therefore never receive negative offsets, zero page sizes, or unbounded
/// page-size requests from HTTP clients.
/// </summary>
public sealed class PaginationValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, string[]>();
        ValidatePositiveInteger(context, "page", maximum: int.MaxValue, errors);
        ValidatePositiveInteger(context, "pageSize", maximum: 100, errors);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        await next();
    }

    private static void ValidatePositiveInteger(ActionExecutingContext context, string name,
        int maximum, IDictionary<string, string[]> errors)
    {
        if (!context.HttpContext.Request.Query.TryGetValue(name, out var raw))
            return;

        if (!int.TryParse(raw, out var value) || value < 1 || value > maximum)
        {
            errors[name] =
            [name == "pageSize" ? "Page size must be between 1 and 100." : "Page must be greater than zero."];
        }
    }
}
