using System.Reflection;
using Asp.Versioning;
using ClaudyGod.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClaudyGod.API.Tests.Security;

public class EndpointConventionTests
{
    private static readonly Type[] ControllerTypes = typeof(AdminController).Assembly
        .GetTypes()
        .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
        .ToArray();

    [Fact]
    public void ControllerLevelAnonymousMetadata_MustNotMixWithAuthorizedActions()
    {
        var violations = ControllerTypes
            .Where(IsAnonymous)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttributes(inherit: true).OfType<AuthorizeAttribute>().Any())
                .Select(method => $"{type.Name}.{method.Name}"))
            .ToArray();

        violations.Should().BeEmpty(
            "controller-level anonymous metadata overrides method-level authorization");
    }

    [Fact]
    public void AnonymousMutationEndpoints_MustHaveAnExplicitRateLimitPolicy()
    {
        var violations = ControllerTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(IsHttpMutation)
                .Where(method => IsAnonymous(type) || IsAnonymous(method))
                .Where(method => !HasRateLimit(type) && !HasRateLimit(method))
                .Select(method => $"{type.Name}.{method.Name}"))
            .ToArray();

        violations.Should().BeEmpty(
            "every anonymous state-changing endpoint needs endpoint-specific abuse protection");
    }

    private static bool IsAnonymous(MemberInfo member) =>
        member.GetCustomAttributes(inherit: true).OfType<IAllowAnonymous>().Any();

    private static bool HasRateLimit(MemberInfo member) =>
        member.GetCustomAttributes(inherit: true).OfType<EnableRateLimitingAttribute>().Any();

    private static bool IsHttpMutation(MethodInfo method) =>
        method.GetCustomAttributes(inherit: true).Any(attribute => attribute is
            HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute);
}
