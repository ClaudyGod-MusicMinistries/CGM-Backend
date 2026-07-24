using System.Text.RegularExpressions;

namespace ClaudyGod.Application.Common.Utilities;

/// <summary>
/// Generates a slug matching Common/Validators/SlugValidator's format
/// (lowercase letters, numbers, and hyphens only) — used wherever a slug is
/// derived server-side from a display name rather than typed by the caller
/// (e.g. blog categories/tags, where asking the admin to hand-craft a slug
/// for a simple taxonomy term is unnecessary friction).
/// </summary>
public static class SlugHelper
{
    public static string Generate(string input)
    {
        var lowered = input.Trim().ToLowerInvariant();
        var hyphenated = Regex.Replace(lowered, @"[^a-z0-9]+", "-");
        return hyphenated.Trim('-');
    }
}
