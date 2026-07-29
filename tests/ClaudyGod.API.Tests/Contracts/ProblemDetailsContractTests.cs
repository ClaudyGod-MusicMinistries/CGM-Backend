using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClaudyGod.API.Tests.Infrastructure;
using FluentAssertions;

namespace ClaudyGod.API.Tests.Contracts;

public class ProblemDetailsContractTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProblemDetailsContractTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task InvalidMediaType_ReturnsStableValidationContract()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/media?type=unsupported");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        body.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        body.GetProperty("errors").GetProperty("type")[0].GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MissingBearerToken_ReturnsStableAuthenticationContract()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/admin/dashboard");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("code").GetString().Should().Be("AUTHENTICATION_REQUIRED");
        body.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
