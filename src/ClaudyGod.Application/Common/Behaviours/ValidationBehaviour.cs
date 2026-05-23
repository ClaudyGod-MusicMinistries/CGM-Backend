using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Common.Behaviours;

public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        var errors = failures
            .GroupBy(f => NormalizePropertyName(f.PropertyName))
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        throw new Domain.Exceptions.ValidationException(errors);
    }

    // Strips wrapper prefixes like "Request.", "Command.", "Dto." so the
    // frontend receives clean field names: "firstName" not "Request.FirstName".
    private static readonly Regex _prefixPattern =
        new(@"^(?:Request|Command|Dto|Query)\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string NormalizePropertyName(string propertyName)
    {
        var normalized = _prefixPattern.Replace(propertyName, string.Empty);
        // Convert PascalCase to camelCase for consistent frontend field mapping
        return string.IsNullOrEmpty(normalized)
            ? normalized
            : char.ToLowerInvariant(normalized[0]) + normalized[1..];
    }
}
