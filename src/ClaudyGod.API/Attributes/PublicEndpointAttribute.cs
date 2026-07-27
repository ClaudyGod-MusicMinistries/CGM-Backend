namespace ClaudyGod.API.Attributes;

/// <summary>
/// Explicitly marks an endpoint as public: it bypasses both the API-key gate and
/// ASP.NET authorization. Endpoints which are anonymous but still require a
/// server-to-server API key should use [AllowAnonymous] instead.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PublicEndpointAttribute : Attribute, Microsoft.AspNetCore.Authorization.IAllowAnonymous
{
}
