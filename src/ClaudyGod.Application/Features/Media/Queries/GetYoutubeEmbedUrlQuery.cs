using FluentValidation;
using MediatR;

namespace ClaudyGod.Application.Features.Media.Queries;

public record GetYoutubeEmbedUrlQuery(
    string VideoId,
    bool Autoplay = false,
    bool Controls = true,
    bool ModestBranding = true) : IRequest<YoutubeEmbedDto>;

public record YoutubeEmbedDto(
    string VideoId,
    string EmbedUrl,
    string Provider,
    int ExpiresIn,
    DateTime GeneratedAt);

public class GetYoutubeEmbedUrlQueryValidator : AbstractValidator<GetYoutubeEmbedUrlQuery>
{
    public GetYoutubeEmbedUrlQueryValidator()
    {
        // YouTube video IDs are 11 characters: alphanumeric, dash, underscore.
        RuleFor(x => x.VideoId)
            .Matches(@"^[a-zA-Z0-9_-]{11}$")
            .WithMessage("Invalid video ID format (must be 11 alphanumeric characters).");
    }
}

public class GetYoutubeEmbedUrlQueryHandler : IRequestHandler<GetYoutubeEmbedUrlQuery, YoutubeEmbedDto>
{
    private const string YoutubeNoCookieDomain = "youtube-nocookie.com";
    private const string YoutubeEmbedPath = "embed";

    public Task<YoutubeEmbedDto> Handle(GetYoutubeEmbedUrlQuery request, CancellationToken ct)
    {
        var builder = new UriBuilder($"https://{YoutubeNoCookieDomain}/{YoutubeEmbedPath}/{request.VideoId}");

        var query = new Dictionary<string, string>
        {
            { "autoplay", request.Autoplay ? "1" : "0" },
            { "controls", request.Controls ? "1" : "0" },
            { "modestbranding", request.ModestBranding ? "1" : "0" },
            { "rel", "0" },            // Prevent related videos
            { "fs", "1" },             // Allow fullscreen
            { "iv_load_policy", "3" }, // Hide annotations
        };

        builder.Query = string.Join("&", query.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var dto = new YoutubeEmbedDto(
            VideoId: request.VideoId,
            EmbedUrl: builder.Uri.ToString(),
            Provider: YoutubeNoCookieDomain,
            ExpiresIn: 3600,
            GeneratedAt: DateTime.UtcNow);

        return Task.FromResult(dto);
    }
}
