using System.Net;
using ClaudyGod.API.Tests.Infrastructure;
using FluentAssertions;

namespace ClaudyGod.API.Tests.Operations;

public class HealthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthEndpointTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Liveness_DoesNotRequireAuthenticationOrExternalDependencies()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
