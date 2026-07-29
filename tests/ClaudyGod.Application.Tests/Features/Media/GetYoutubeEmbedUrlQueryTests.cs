using ClaudyGod.Application.Features.Media.Queries;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ClaudyGod.Application.Tests.Features.Media;

public class GetYoutubeEmbedUrlQueryValidatorTests
{
    private readonly GetYoutubeEmbedUrlQueryValidator _validator = new();

    [Theory]
    [InlineData("dQw4w9WgXcQ")]
    [InlineData("abc-DEF_123")]
    public void Passes_ForValidElevenCharacterIds(string videoId) =>
        _validator.TestValidate(new GetYoutubeEmbedUrlQuery(videoId))
            .ShouldNotHaveValidationErrorFor(x => x.VideoId);

    [Theory]
    [InlineData("short")]
    [InlineData("way-too-long-id-here")]
    [InlineData("has spaces!")]
    public void Fails_ForInvalidIds(string videoId) =>
        _validator.TestValidate(new GetYoutubeEmbedUrlQuery(videoId))
            .ShouldHaveValidationErrorFor(x => x.VideoId);
}

public class GetYoutubeEmbedUrlQueryHandlerTests
{
    [Fact]
    public async Task Handle_BuildsNoCookieEmbedUrlWithExpectedQueryParams()
    {
        var handler = new GetYoutubeEmbedUrlQueryHandler();

        var result = await handler.Handle(
            new GetYoutubeEmbedUrlQuery("dQw4w9WgXcQ", Autoplay: true, Controls: false, ModestBranding: true),
            CancellationToken.None);

        result.VideoId.Should().Be("dQw4w9WgXcQ");
        result.Provider.Should().Be("youtube-nocookie.com");
        result.EmbedUrl.Should().Contain("youtube-nocookie.com/embed/dQw4w9WgXcQ");
        result.EmbedUrl.Should().Contain("autoplay=1");
        result.EmbedUrl.Should().Contain("controls=0");
        result.EmbedUrl.Should().Contain("rel=0");
    }
}
