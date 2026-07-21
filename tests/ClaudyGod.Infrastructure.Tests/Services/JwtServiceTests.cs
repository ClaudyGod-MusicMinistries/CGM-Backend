using ClaudyGod.Domain.Entities;
using ClaudyGod.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ClaudyGod.Infrastructure.Tests.Services;

public class JwtServiceTests
{
    private const string ValidKey = "this-is-a-test-signing-key-that-is-at-least-32-bytes-long";

    private static JwtService NewService(string? key = ValidKey) =>
        new(BuildConfig(key));

    private static IConfiguration BuildConfig(string? key) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = "ClaudyGod.API",
                ["Jwt:Audience"] = "ClaudyGod.Client",
                ["Jwt:AccessTokenExpiryMinutes"] = "60",
                ["Jwt:RefreshTokenExpiryDays"] = "7",
            })
            .Build();

    private static User TestUser() =>
        User.Create("peter", "peter@claudygod.org", "hash");

    [Fact]
    public void Constructor_ThrowsWhenKeyMissing()
    {
        var act = () => NewService(key: null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_ThrowsWhenKeyTooShort()
    {
        var act = () => NewService(key: "short-key");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GenerateAccessToken_ProducesTokenThatValidatesSuccessfully()
    {
        var service = NewService();
        var token = service.GenerateAccessToken(TestUser());

        var principal = service.ValidateToken(token);

        principal.Should().NotBeNull();
        principal!.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_RejectsTamperedToken()
    {
        var service = NewService();
        var token = service.GenerateAccessToken(TestUser());
        var tampered = token[..^2] + (token[^2] == 'A' ? "B" : "A") + token[^1];

        service.ValidateToken(tampered).Should().BeNull();
    }

    [Fact]
    public void ValidateToken_RejectsTokenSignedWithDifferentKey()
    {
        var issuer = NewService(key: "issuer-signing-key-that-is-at-least-32-bytes-long!!");
        var verifier = NewService(key: "a-completely-different-signing-key-32-bytes-plus");

        var token = issuer.GenerateAccessToken(TestUser());

        verifier.ValidateToken(token).Should().BeNull();
    }

    [Fact]
    public void ValidateToken_RejectsGarbageInput()
    {
        var service = NewService();
        service.ValidateToken("not-a-jwt-at-all").Should().BeNull();
    }
}
