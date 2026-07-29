using FluentValidation;

namespace ClaudyGod.Application.Common.Validators;

public static class SlugValidator
{
    /// <summary>Requires a non-empty, lowercase-alphanumeric-with-hyphens slug (max 500 chars).</summary>
    public static IRuleBuilderOptions<T, string> ValidSlug<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.NotEmpty().MaximumLength(500)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug may only contain lowercase letters, numbers, and hyphens.");
}
