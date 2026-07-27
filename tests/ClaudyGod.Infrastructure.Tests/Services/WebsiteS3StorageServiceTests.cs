using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Infrastructure.Configuration;
using ClaudyGod.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ClaudyGod.Infrastructure.Tests.Services;

public class WebsiteS3StorageServiceTests
{
    [Fact]
    public void GetPublicUrl_ProjectOrigin_BuildsSupabasePublicObjectUrl()
    {
        var service = CreateService("https://project.supabase.co");

        var result = service.GetPublicUrl("website/images/example.jpg");

        result.Should().Be(
            "https://project.supabase.co/storage/v1/object/public/website-uploads/website/images/example.jpg");
    }

    [Fact]
    public void GetPublicUrl_CompleteBucketUrl_DoesNotDuplicatePublicObjectPath()
    {
        var service = CreateService(
            "https://project.supabase.co/storage/v1/object/public/website-uploads/");

        var result = service.GetPublicUrl("/website/images/example.jpg");

        result.Should().Be(
            "https://project.supabase.co/storage/v1/object/public/website-uploads/website/images/example.jpg");
    }

    private static WebsiteS3StorageService CreateService(string publicBaseUrl)
    {
        var options = Options.Create(new WebsiteStorageOptions
        {
            S3Endpoint = "https://project.supabase.co/storage/v1/s3",
            S3Region = "us-east-1",
            S3AccessKeyId = "test-access-key",
            S3SecretAccessKey = "test-secret-key",
            Bucket = "website-uploads",
            PublicBaseUrl = publicBaseUrl,
        });

        return new WebsiteS3StorageService(
            options,
            Substitute.For<IApplicationDbContext>(),
            Substitute.For<ILogger<WebsiteS3StorageService>>());
    }
}
