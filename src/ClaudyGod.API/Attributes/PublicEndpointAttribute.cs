namespace ClaudyGod.API.Attributes;

/// <summary>
/// Explicitly marks a read or mutation endpoint as publicly accessible.
/// This is a semantic alias for ASP.NET Core's anonymous-endpoint contract;
/// all other endpoints are protected by the secure fallback policy.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PublicEndpointAttribute : Attribute, Microsoft.AspNetCore.Authorization.IAllowAnonymous
{
}
