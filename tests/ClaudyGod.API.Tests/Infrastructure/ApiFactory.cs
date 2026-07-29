using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaudyGod.API.Tests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "integration-test-jwt-signing-key-at-least-32-bytes";
    public const string JwtIssuer = "ClaudyGod.API.Tests";
    public const string JwtAudience = "ClaudyGod.API.Tests.Client";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=test");
        builder.UseSetting("Redis:ConnectionString", "127.0.0.1:1,abortConnect=false");
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Storage:Website:S3Endpoint", "https://storage.invalid/s3");
        builder.UseSetting("Storage:Website:S3Region", "us-east-1");
        builder.UseSetting("Storage:Website:S3AccessKeyId", "integration-test-access-key");
        builder.UseSetting("Storage:Website:S3SecretAccessKey", "integration-test-secret-key");
        builder.UseSetting("Storage:Website:Bucket", "integration-tests");
        builder.UseSetting("Storage:Website:PublicBaseUrl", "https://storage.invalid");
    }
}
