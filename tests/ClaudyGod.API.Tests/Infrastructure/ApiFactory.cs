using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClaudyGod.API.Tests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "integration-test-api-key-at-least-32-bytes";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=test",
                ["Redis:ConnectionString"] = "127.0.0.1:1,abortConnect=false",
                ["Jwt:Key"] = "integration-test-jwt-signing-key-at-least-32-bytes",
                ["Jwt:Issuer"] = "ClaudyGod.API.Tests",
                ["Jwt:Audience"] = "ClaudyGod.API.Tests.Client",
                ["Security:ApiKeys:0"] = ApiKey,
                ["Storage:Website:S3Endpoint"] = "https://storage.invalid/s3",
                ["Storage:Website:S3Region"] = "us-east-1",
                ["Storage:Website:S3AccessKeyId"] = "integration-test-access-key",
                ["Storage:Website:S3SecretAccessKey"] = "integration-test-secret-key",
                ["Storage:Website:Bucket"] = "integration-tests",
                ["Storage:Website:PublicBaseUrl"] = "https://storage.invalid",
            });
        });
    }
}
