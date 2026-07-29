using System.Net;
using ClaudyGod.API.Tests.Infrastructure;
using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using ClaudyGod.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace ClaudyGod.API.Tests.Security;

public class AuthorizationBoundaryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthorizationBoundaryTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task PublicEndpoint_DoesNotRequireJwt()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/media/youtube/dQw4w9WgXcQ");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminEndpoint_WithoutJwt_IsRejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SpoofedActorHeaders_DoNotAuthenticateAdminRequest()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-actor-id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("x-actor-email", "attacker@example.com");

        var response = await client.GetAsync("/api/v1.0/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedNonAdmin_IsForbiddenFromAdminEndpoint()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(UserRole.User));

        var response = await client.GetAsync("/api/v1.0/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ValidBearerToken_CanReadOwnIdentity()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(UserRole.User));

        var response = await client.GetAsync("/api/v1.0/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OwnIdentity_WithoutBearerToken_IsRejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void ForwardedHeaders_TrustNoProxyByDefault()
    {
        var options = _factory.Services
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        options.KnownNetworks.Should().BeEmpty();
        options.KnownProxies.Should().BeEmpty();
        options.ForwardLimit.Should().Be(1);
    }

    private static string CreateToken(UserRole role)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = ApiFactory.JwtKey,
                ["Jwt:Issuer"] = ApiFactory.JwtIssuer,
                ["Jwt:Audience"] = ApiFactory.JwtAudience,
            })
            .Build();
        var user = User.Create("test-user", "test@example.com", "unused", role);
        return new JwtService(configuration).GenerateAccessToken(user).Token;
    }
}
