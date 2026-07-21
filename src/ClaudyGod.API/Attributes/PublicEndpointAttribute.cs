namespace ClaudyGod.API.Attributes;

/// <summary>
/// Marks a controller or action as not requiring the x-api-key header, checked by
/// ApiKeyMiddleware via endpoint metadata. Prefer applying this next to the route it
/// describes rather than maintaining a separate path list, so a new public route can't
/// be silently left unprotected (or a protected one silently left open) by omission.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PublicEndpointAttribute : Attribute
{
}
