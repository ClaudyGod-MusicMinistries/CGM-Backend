using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ClaudyGod.API.Authentication;

public sealed class AdminGatewayAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "AdminGateway";
    public const string ApiKeyHeader = "x-api-key";
    public const string ActorIdHeader = "x-actor-id";
    public const string ActorEmailHeader = "x-actor-email";

    private readonly IConfiguration _configuration;

    public AdminGatewayAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var suppliedValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var expected = _configuration["AdminGateway:ApiKey"];
        var supplied = suppliedValues.ToString();

        if (string.IsNullOrWhiteSpace(expected) || !KeysMatch(expected, supplied))
            return Task.FromResult(AuthenticateResult.Fail("Invalid admin gateway credentials."));

        var actorId = Request.Headers[ActorIdHeader].ToString().Trim();
        var actorEmail = Request.Headers[ActorEmailHeader].ToString().Trim();
        if (!Guid.TryParse(actorId, out _) || string.IsNullOrWhiteSpace(actorEmail) || actorEmail.Length > 320)
            return Task.FromResult(AuthenticateResult.Fail("Valid gateway actor identity is required."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, actorId),
            new Claim(ClaimTypes.Email, actorEmail),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("authentication_source", "admin_gateway"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    private static bool KeysMatch(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
