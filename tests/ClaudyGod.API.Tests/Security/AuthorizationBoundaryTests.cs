using System.Net;
using ClaudyGod.API.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClaudyGod.API.Tests.Security;

public class AuthorizationBoundaryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthorizationBoundaryTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task PublicEndpoint_DoesNotRequireApiKeyOrJwt()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/media/youtube/dQw4w9WgXcQ");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminEndpoint_WithoutApiKey_IsRejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_WithApiKeyButWithoutJwt_IsRejected()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", ApiFactory.ApiKey);

        var response = await client.GetAsync("/api/v1.0/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SpoofedActorHeaders_DoNotAuthenticateAdminRequest()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", ApiFactory.ApiKey);
        client.DefaultRequestHeaders.Add("x-actor-id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("x-actor-email", "attacker@example.com");

        var response = await client.GetAsync("/api/v1.0/admin/dashboard");

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
}
